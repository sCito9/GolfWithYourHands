import json

with open("New Controls.inputactions", "r", encoding="utf-8") as f:
    try:
        json.load(f)
        print("✅ JSON ist gültig.")
    except json.JSONDecodeError as e:
        print(f"❌ JSON-Fehler: {e}")

