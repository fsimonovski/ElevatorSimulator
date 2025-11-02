# Elevator Control System (C# / .NET)

A simple yet structured simulation of an **elementary elevator control system**, written in **C# (.NET)**.  
This project demonstrates core design principles — clean code, event-driven updates, asynchronous processing, and a minimal front-end with **SignalR** for real-time visualization.

---

## Overview

This system simulates **4 elevators** operating in a **10-floor building**.  
Elevators handle random and user-generated requests, moving passengers between floors with realistic timing rules:

-  **10 seconds** to move between floors  
-  **10 seconds** to load/unload passengers  
-  **Directional persistence**: an elevator continues in its current direction until no more passengers need service that way  
-  A **simple assignment algorithm** picks the best elevator for each new request  

---

##  Architecture

```mermaid
flowchart TD
    subgraph UI[" Web UI (Razor + JS)"]
        style UI fill:#2d2d2d,stroke:#444,stroke-width:2px
        A[User Request] -->|SignalR Hub Call| B(RequestElevator)
    end

    subgraph Backend[" ASP.NET Core Backend"]
        style Backend fill:#212121,stroke:#333,stroke-width:2px
        C{ElevatorControlSystem}
        style C fill:#263238,stroke:#607d8b,stroke-width:2px
        D[Elevator 1..4]
        style D fill:#37474f,stroke:#78909c,stroke-width:2px
        E[StateChanged Event]
        style E fill:#2e7d32,stroke:#388e3c,stroke-width:2px
        F((Simulation Service))
        style F fill:#6a1b9a,stroke:#ab47bc,stroke-width:2px
        G[SignalR Hub]
        style G fill:#f44336,stroke:#d32f2f,stroke-width:2px
    end

    A -->|SignalR Hub Call| B(RequestElevator)
    B --> C
    C -->|Assigns| D
    D --> E
    C --> F
    F -->|Tick Loop| D
    E -->|Broadcast| G
    G -->|Push Update| UI

```

---

## Running the Simulation

### 1. Clone the repository
```bash
git clone https://github.com/<your-username>/ElevatorApp.git
cd ElevatorApp
```

### 2. Run the web project
```bash
dotnet run --project ElevatorApp.Web
```

### 3. Open your browser
Visit the application at:
```
https://localhost:7076
```
### 4. Start the Simulation
Click **“Start Simulation”** to start booking elevator requests,  
or manually select floors and click **“Call Elevator”** to create your own.
