using Microsoft.AspNetCore.Mvc;
using Docker.DotNet;
using Docker.DotNet.Models;
using System.Collections.Concurrent;
using MongoDB.Bson.Serialization.Attributes;

namespace SGServer.Controllers
{
    /// <summary>
    /// Controller for managing Docker container instances. Uses the URL path <code> api/server </code>
    /// Implements Docker-outside-of-Docker (DooD) functionality with a limit of 4 concurrent containers.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ServerController : ControllerBase
    {
        private readonly ILogger<ServerController> _logger;
        private readonly DockerClient _dockerClient;
        
        // In-memory storage for active Docker containers
        private static readonly ConcurrentDictionary<string, ContainerStatus> ActiveContainers = new();
        
        // Maximum number of containers allowed to run concurrently
        private const int MaxContainers = 8;

        // Fixed container configuration
        private const string ContainerImage = "dedicated-server";
        private const int ExposedPort = 7777;
        private const int BaseHostPort = 9000;

        /// <summary>
        /// Container status tracking class
        /// </summary>
        public class ContainerStatus
        {
            [BsonElement("Id")]
            public string Id { get; set; } = string.Empty;
            [BsonElement("Name")]
            public string Name { get; set; } = string.Empty;
            [BsonElement("Image")]
            public string Image { get; set; } = string.Empty;
            [BsonElement("Status")]
            public string Status { get; set; } = string.Empty;
            [BsonElement("CreatedAt")]
            public DateTime CreatedAt { get; set; }
            [BsonElement("GameId")]
            public string GameId { get; set; } = string.Empty;
            [BsonElement("HostPort")]
            public int HostPort { get; set; }
        }

        /// <summary>
        /// Constructor for ServerController
        /// </summary>
        /// <param name="logger">Logger for logging</param>
        public ServerController(ILogger<ServerController> logger)
        {
            _logger = logger;
            
            // Connect to the Docker daemon using Unix socket (mounted from the host)
            // For Windows hosts, this may need to be adjusted to use named pipes
            var endpoint = "unix:///var/run/docker.sock";
            _dockerClient = new DockerClientConfiguration(new Uri(endpoint))
                .CreateClient();
            
            // Initialize container list on startup
            _ = InitializeContainerListAsync();
        }

        /// <summary>
        /// Initialize the list of active containers on startup
        /// </summary>
        private async Task InitializeContainerListAsync()
        {
            try
            {
                var containers = await _dockerClient.Containers.ListContainersAsync(
                    new ContainersListParameters { All = true });
                
                foreach (var container in containers)
                {
                    // Only track containers created by this controller
                    if (!container.Names.Any(n => n.StartsWith("/sgserver-"))) continue;
                    
                    var status = new ContainerStatus
                    {
                        Id = container.ID,
                        Name = container.Names.FirstOrDefault()?.TrimStart('/') ?? string.Empty,
                        Image = container.Image,
                        Status = container.Status,
                        CreatedAt = container.Created
                    };
                        
                    // Try to extract gameId from environment variables
                    var inspect = await _dockerClient.Containers.InspectContainerAsync(container.ID);
                    var envVars = inspect.Config?.Env;
                    
                    var gameIdVar = envVars?.FirstOrDefault(e => e.StartsWith("GAME_ID="));
                    
                    if (gameIdVar != null)
                    {
                        status.GameId = gameIdVar.Substring("GAME_ID=".Length);
                    }

                    // Try to extract host port from port bindings
                    var portBinding = inspect.NetworkSettings?.Ports?.FirstOrDefault(p => 
                        p.Key.StartsWith($"{ExposedPort}/")).Value;
                            
                    if (portBinding != null && portBinding.Any() && !string.IsNullOrEmpty(portBinding[0].HostPort))
                    {
                        if (int.TryParse(portBinding[0].HostPort, out var hostPort))
                        {
                            status.HostPort = hostPort;
                        }
                    }

                    ActiveContainers.TryAdd(container.ID, status);
                }
                
                _logger.LogInformation("Server: Initialized with {Count} active containers", ActiveContainers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server: Failed to initialize container list");
            }
        }

        /// <summary>
        /// Get all active Docker containers when calling <code> GET api/server </code>
        /// </summary>
        /// <returns>HTTP Ok with all active containers as the body</returns>
        [HttpGet]
        public IActionResult Get()
        {
            _logger.LogInformation("Server: GET all active containers");
            var containers = ActiveContainers.Values.ToList();
            return Ok(containers);
        }

        /// <summary>
        /// Get a specific Docker container by id when calling <code> GET api/server/{id} </code>
        /// </summary>
        /// <param name="id">The id of the container</param>
        /// <returns>HTTP Ok/NotFound</returns>
        [HttpGet("{id}")]
        public IActionResult Get(string id)
        {
            _logger.LogInformation("Server: GET container with ID: {Id}", id);

            if (!ActiveContainers.TryGetValue(id, out var container))
            {
                _logger.LogWarning("Server: NotFound. Container with ID: {Id} not found.", id);
                return NotFound();
            }

            _logger.LogInformation("Server: Ok. Container with ID: {Id} found.", id);
            return Ok(container);
        }

        /// <summary>
        /// Get container by game ID when calling <code> GET api/server/game/{gameId} </code>
        /// </summary>
        /// <param name="gameId">The game ID to find container for</param>
        /// <returns>HTTP Ok/NotFound</returns>
        [HttpGet("game/{gameId}")]
        public IActionResult GetByGameId(string gameId)
        {
            _logger.LogInformation("Server: GET container for game ID: {GameId}", gameId);

            var container = ActiveContainers.Values
                .FirstOrDefault(c => c.GameId.Equals(gameId, StringComparison.OrdinalIgnoreCase));
                
            if (container == null)
            {
                _logger.LogWarning("Server: NotFound. Container for game ID: {GameId} not found.", gameId);
                return NotFound();
            }

            _logger.LogInformation("Server: Ok. Container for game ID: {GameId} found.", gameId);
            return Ok(container);
        }

        /// <summary>
        /// Create a new Docker container for a game when calling <code>GET api/server/create/{gameId}</code>.
        /// If there are already 4 containers running, returns an error.
        /// </summary>
        /// <param name="gameId">The game ID to create a container for</param>
        /// <returns>HTTP Created/BadRequest/TooManyRequests</returns>
        [HttpGet("create/{gameId}")]
        public async Task<IActionResult> CreateContainer(string gameId)
        {
            _logger.LogInformation("Server: Creating new container for game ID: {GameId}", gameId);

            // Check if we've reached the maximum number of containers
            if (ActiveContainers.Count >= MaxContainers)
            {
                _logger.LogWarning("Server: TooManyRequests. Maximum number of containers ({Max}) already running.", MaxContainers);
                return StatusCode(StatusCodes.Status429TooManyRequests, $"Maximum number of containers ({MaxContainers}) already running.");
            }
            
            // Check if a container for this game already exists
            var existingContainer = ActiveContainers.Values
                .FirstOrDefault(c => c.GameId.Equals(gameId, StringComparison.OrdinalIgnoreCase));
                
            if (existingContainer != null)
            {
                _logger.LogWarning("Server: Conflict. Container for game ID: {GameId} already exists.", gameId);
                return Conflict($"Container for game ID {gameId} already exists.");
            }

            try
            {
                // Generate a unique name for the container
                var containerName = $"sgserver-{gameId}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                
                // Find an available host port
                var usedPorts = ActiveContainers.Values.Select(c => c.HostPort).ToHashSet();
                int hostPort = BaseHostPort;
                while (usedPorts.Contains(hostPort))
                {
                    hostPort++;
                }
                
                // Create container
                var createParams = new CreateContainerParameters
                {
                    Image = ContainerImage,
                    Name = containerName,
                    ExposedPorts = new Dictionary<string, EmptyStruct>
                    {
                        { $"{ExposedPort}/tcp", default },
                        { $"{ExposedPort}/udp", default }
                    },
                    Env = new List<string>
                    {
                        $"GAME_ID={gameId}",
                        "ASPNETCORE_URLS=http://+:8080"
                    },
                    HostConfig = new HostConfig
                    {
                        PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            {
                                $"{ExposedPort}/tcp",
                                new[] { new PortBinding { HostPort = hostPort.ToString() } }
                            },
                            {
                                $"{ExposedPort}/udp",
                                new[] { new PortBinding { HostPort = hostPort.ToString() } }
                            }
                        },
                        PublishAllPorts = true
                    }
                };

                // Create the container
                var response = await _dockerClient.Containers.CreateContainerAsync(createParams);
                
                if (string.IsNullOrEmpty(response.ID))
                {
                    _logger.LogError("Server: Error. Failed to create container.");
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to create container.");
                }

                // Start the container
                var started = await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());
                if (!started)
                {
                    _logger.LogError("Server: Error. Failed to start container {Id}.", response.ID);
                    // Try to clean up the created but unstarted container
                    await _dockerClient.Containers.RemoveContainerAsync(response.ID, new ContainerRemoveParameters { Force = true });
                    return StatusCode(StatusCodes.Status500InternalServerError, "Failed to start container.");
                }

                // Track the new container
                var containerInfo = new ContainerStatus
                {
                    Id = response.ID,
                    Name = containerName,
                    Image = ContainerImage,
                    Status = "running",
                    CreatedAt = DateTime.UtcNow,
                    GameId = gameId,
                    HostPort = hostPort
                };
                
                ActiveContainers.TryAdd(response.ID, containerInfo);
                
                _logger.LogInformation("Server: Created. New container with ID: {Id} created for game: {GameId}", 
                    response.ID, gameId);
                    
                return CreatedAtAction(nameof(Get), new { id = response.ID }, containerInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server: Error creating container for game ID: {GameId}", gameId);
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error creating container: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop and remove a Docker container when calling <code>DELETE api/server/{id}</code>
        /// </summary>
        /// <param name="id">The id of the container to stop and remove</param>
        /// <returns>HTTP NoContent/NotFound/InternalServerError</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContainer(string id)
        {
            _logger.LogInformation("Server: DELETE. Removing container with ID: {Id}", id);

            if (!ActiveContainers.TryGetValue(id, out _))
            {
                _logger.LogWarning("Server: NotFound. Container with ID: {Id} not found for deletion.", id);
                return NotFound();
            }

            try
            {
                // Stop the container first (with a 30 second timeout)
                await _dockerClient.Containers.StopContainerAsync(id, new ContainerStopParameters { WaitBeforeKillSeconds = 30 });
                
                // Then remove it
                await _dockerClient.Containers.RemoveContainerAsync(id, new ContainerRemoveParameters { Force = true });
                
                // Remove from tracking
                ActiveContainers.TryRemove(id, out _);
                
                _logger.LogInformation("Server: Deleted. Container with ID: {Id} stopped and removed.", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server: Error deleting container {Id}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error deleting container: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Stop and remove a Docker container by game ID when calling <code>DELETE api/server/game/{gameId}</code>
        /// </summary>
        /// <param name="gameId">The game ID to stop container for</param>
        /// <returns>HTTP NoContent/NotFound/InternalServerError</returns>
        [HttpDelete("game/{gameId}")]
        public async Task<IActionResult> DeleteContainerByGameId(string gameId)
        {
            _logger.LogInformation("Server: DELETE. Removing container for game ID: {GameId}", gameId);

            var container = ActiveContainers.Values
                .FirstOrDefault(c => c.GameId.Equals(gameId, StringComparison.OrdinalIgnoreCase));
                
            if (container == null)
            {
                _logger.LogWarning("Server: NotFound. Container for game ID: {GameId} not found.", gameId);
                return NotFound();
            }

            return await DeleteContainer(container.Id);
        }

        /// <summary>
        /// Get detailed diagnostic information about a container when calling <code> GET api/server/diagnose/{id} </code>
        /// </summary>
        /// <param name="id">The id of the container</param>
        /// <returns>HTTP Ok/NotFound with detailed diagnostic information</returns>
        [HttpGet("diagnose/{id}")]
        public async Task<IActionResult> DiagnoseContainer(string id)
        {
            _logger.LogInformation("Server: DIAGNOSE container with ID: {Id}", id);

            if (!ActiveContainers.TryGetValue(id, out var container))
            {
                _logger.LogWarning("Server: NotFound. Container with ID: {Id} not found for diagnosis.", id);
                return NotFound();
            }
            
            try
            {
                // Get detailed container information
                var inspectResult = await _dockerClient.Containers.InspectContainerAsync(id);
                
                // Get container logs
                var logsParameters = new ContainerLogsParameters
                {
                    ShowStdout = true,
                    ShowStderr = true,
                    Tail = "100"  // Get the last 100 lines
                };
                
                var logStream = await _dockerClient.Containers.GetContainerLogsAsync(id, logsParameters);
                var logs = new List<string>();
                
                using (var reader = new StreamReader(logStream))
                {
                    while (!reader.EndOfStream)
                    {
                        logs.Add(await reader.ReadLineAsync() ?? string.Empty);
                    }
                }
                
                // Create a diagnostic report
                var diagnosticReport = new
                {
                    ContainerInfo = container,
                    NetworkSettings = inspectResult.NetworkSettings,
                    State = inspectResult.State,
                    Ports = inspectResult.NetworkSettings?.Ports,
                    Logs = logs,
                    PortBindings = inspectResult.HostConfig?.PortBindings,
                    DiagnosticHints = new List<string>
                    {
                        "Check if the container is running",
                        "Verify port bindings are correct",
                        "Review logs for application errors",
                        "Ensure network settings allow proper communication"
                    }
                };
                
                _logger.LogInformation("Server: Diagnostics generated for container: {Id}", id);
                return Ok(diagnosticReport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server: Error generating diagnostics for container {Id}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error generating diagnostics: {ex.Message}");
            }
        }
    }
}
