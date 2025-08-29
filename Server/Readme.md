# Social Game Server

## Docker

### Container Starten und stoppen

> #TODO: Der Server ist bis jetzt nur auf Linux getestet. Falls es dort noch nicht funktioniert, könnte WSL funktionieren. Ich prüfe das noch.

Der Server läuft am einfachsten über Docker Compose. Das kümmert sich um das Setup von .Net und MongoDB und die Kommunikation zwischen den beiden. Dafür einfach 

```bash
sudo docker compose up --build
```

ausführen. So läuft er im Vordergrund. Um ihn zu stoppen einfach `Ctrl+C` drücken. Um ihn im Hintergrund laufen zu lassen, könnt ihr die Option `-d` verwenden. Dann einfach mit:

```bash
sudo docker compose down
```

wieder stoppen.

### Cleanup

Docker speichert die Daten in MongoDB in einem Docker Volume. Heißt, die Daten werden nicht gelöscht, wenn der Container gestoppt wird. Das kann dazu führen, dass die lokale Datenbank nach einem Pull nicht mehr mit dem Code funktioniert und MongoDB meckert. Um eure lokalen Daten aufzuräumen einmal

```bash
sudo docker compose down --volumes
```

ausführen, dann wird alles was Docker Compose erstellt hat gelöscht.


## API

Die API wird über `https://localhost:8443/api` bereitgestellt. Wenn ihr die API testen wollt, könnt ihr curl verwenden.

> Wichtig: in `Program.cs` kann die Variable `REQUIRE_API_KEY` gesetzt werden. Dann müsst ihr für den Zugriff den Header `X-API-Key: secret-api-key` setzen. Der API Key kann in `appsettings.json` geändert werden.

Hier einige Beispiele:

```bash
# GET anfragen
curl -k https://localhost:8443/api/course
# GET mit API Key
curl -k -H "X-API-Key: secret-api-key" https://localhost:8443/api/course

# POST anfragen
curl -k -X POST \
    -H "Content-Type: application/json" \
    -d '{"name": "Test Course", "description": "Test Course Description"}' \
    https://localhost:8443/api/course

# PUT update (null Felder überschreiben Daten)
curl -k -X PUT \
    -H "Content-Type: application/json" \
    -d '{"id": "6854507662085bbbc38b3383", "name": "New Name", "description": "Test Course Description"}' \
    https://localhost:8443/api/course/6854507662085bbbc38b3383

# DELETE
curl -k -X DELETE https://localhost:8443/api/course/6854507662085bbbc38b3383
```

### Courses

```
GET /api/course             # Alle Courses
GET /api/course/{id}        # Ein Course
POST /api/course            # Neuer Course als Antwort
PUT /api/course/{id}        # Aktualisierter Course als Antwort
DELETE /api/course/{id}     # Keine Rückmeldung
```

### Users

Analog zu Courses
```
GET /api/user
GET /api/user/{id}
POST /api/user
PUT /api/user/{id}
DELETE /api/user/{id}
```

### Friends

Freundschaften und Freundschaftsanfragen werden in den Users gespeichert. Nicht optimal, aber es funktioniert so:

```
# Alle Freundschaften für einen User
GET /api/friend/{userId}    

# Neue Freundschaftsanfrage von userA an userB
POST /api/friend/{userA}/{friendB}

# Alle Freundschaftsanfragen eines Users (redundant)
GET /api/friend/requests/{userId}

# userA akzeptiert Anfrage von userB
POST /api/friend/accept/{userA}/{userB}

# userA lehnt Anfrage von userB ab
POST /api/friend/reject/{userA}/{userB}

# Freundschaft vorbei :(
DELETE /api/friend/remove/{userA}/{userB}
```