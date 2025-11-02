"use strict";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/elevatorHub")
    .configureLogging(signalR.LogLevel.Information)
    .build();

function appendLogRow(origin, dest, note = "Requested", elevatorId = "") {
    const tbody = document.querySelector('#requestLogTable tbody');
    if (!tbody) return;

    const tr = document.createElement('tr');

    const utc = new Date().toISOString().replace('T', ' ').split('.')[0] + " UTC";

    const tsTd = document.createElement('td');
    tsTd.textContent = utc;

    const fromTd = document.createElement('td');
    fromTd.textContent = origin;

    const toTd = document.createElement('td');
    toTd.textContent = dest;

    const elevatorTd = document.createElement('td');
    elevatorTd.textContent = elevatorId ? `#${elevatorId}` : "-";

    const noteTd = document.createElement('td');
    noteTd.textContent = note;

    tr.appendChild(tsTd);
    tr.appendChild(fromTd);
    tr.appendChild(toTd);
    tr.appendChild(elevatorTd);
    tr.appendChild(noteTd);

    tr.classList.add("flash-new");

    // prepend newest row first
    if (tbody.firstChild) {
        tbody.insertBefore(tr, tbody.firstChild);
    } else {
        tbody.appendChild(tr);
    }

    tr.addEventListener("animationend", () => {
        tr.classList.remove("flash-new");
    });
}

const startButton = document.getElementById("startSimButton");
if (startButton) {
    startButton.addEventListener("click", event => {
        connection.invoke("StartSimulation").catch(err => console.error(err.toString()));
        event.target.disabled = true;
        event.target.textContent = "Simulation Running...";
        event.preventDefault();
    });
}

/* NEW: Interactive floor grid wiring
   - Keeps original IDs (#globalOrigin, #globalDest) intact via hidden inputs
   - Updates small "Selected" labels for clarity
*/
function wireFloorGrids() {
    const grids = document.querySelectorAll('.floor-grid');
    grids.forEach(grid => {
        const targetInputId = grid.getAttribute('data-target-input');
        const hiddenInput = document.getElementById(targetInputId);
        const displaySpan = document.getElementById(targetInputId + 'Display');

        grid.addEventListener('click', (e) => {
            const btn = e.target.closest('.floor-btn');
            if (!btn) return;

            // Toggle selected styling within this grid
            grid.querySelectorAll('.floor-btn.selected').forEach(b => b.classList.remove('selected'));
            btn.classList.add('selected');

            const floor = parseInt(btn.getAttribute('data-floor'), 10);
            if (!Number.isNaN(floor)) {
                hiddenInput.value = String(floor);
                if (displaySpan) displaySpan.textContent = floor;
            }
        });
    });
}
wireFloorGrids();

const globalForm = document.getElementById('globalRequestForm');
if (globalForm) {
    globalForm.addEventListener('submit', event => {
        event.preventDefault();

        const originInput = document.getElementById('globalOrigin');
        const destInput = document.getElementById('globalDest');

        const originFloor = parseInt(originInput.value, 10);
        const destFloor = parseInt(destInput.value, 10);

        // NEW: explicit check for "nothing selected" (hidden inputs empty)
        if (Number.isNaN(originFloor) || Number.isNaN(destFloor)) {
            alert("Please select both origin and destination floors.");
            return;
        }

        if (originFloor < 1 || originFloor > 10 ||
            destFloor < 1 || destFloor > 10) {
            alert("Please choose floors between 1 and 10.");
            return;
        }

        if (originFloor === destFloor) {
            alert("Origin and destination floors cannot be the same.");
            return;
        }

        // invoke backend and update logs table
        connection.invoke("RequestElevator", originFloor, destFloor)
            .then(() => {
                appendLogRow(originFloor, destFloor, "Sent");
                // Clear selections after sending to avoid accidental repeats
                clearFloorSelections();
            })
            .catch(err => {
                console.error(err.toString());
                appendLogRow(originFloor, destFloor, "Error");
            });
    });
}

// NEW: helper to clear grid selections & hidden inputs after a successful request
function clearFloorSelections() {
    document.querySelectorAll('.floor-grid').forEach(grid => {
        grid.querySelectorAll('.floor-btn.selected').forEach(btn => btn.classList.remove('selected'));
    });

    const originInput = document.getElementById('globalOrigin');
    const destInput = document.getElementById('globalDest');
    const originDisp = document.getElementById('globalOriginDisplay');
    const destDisp = document.getElementById('globalDestDisplay');

    if (originInput) originInput.value = "";
    if (destInput) destInput.value = "";
    if (originDisp) originDisp.textContent = "None";
    if (destDisp) destDisp.textContent = "None";
}

connection.on("UpdateElevatorState", (elevator) => {
    const elevatorCard = document.getElementById(`elevator-${elevator.id}`);
    if (!elevatorCard) return;

    // Update floor number
    const floorNumberEl = elevatorCard.querySelector('.floor-number');
    if (floorNumberEl) floorNumberEl.textContent = elevator.currentFloor;

    // Update direction arrow
    const arrow = elevatorCard.querySelector('.direction-arrow');
    if (arrow) {
        switch (elevator.direction) {
            case 'Up':
                arrow.textContent = '▲';
                arrow.style.color = 'green';
                break;
            case 'Down':
                arrow.textContent = '▼';
                arrow.style.color = 'orange';
                break;
            default:
                arrow.textContent = '■';
                arrow.style.color = 'gray';
                break;
        }
    }

    const queueList = elevatorCard.querySelector('.request-queue ul');
    if (queueList) {
        queueList.innerHTML = '';
        if (!elevator.requests || elevator.requests.length === 0) {
            const li = document.createElement('li');
            li.className = 'list-group-item';
            li.textContent = 'No active requests';
            queueList.appendChild(li);
        } else {
            elevator.requests.forEach(req => {
                const li = document.createElement('li');
                li.className = 'list-group-item';
                li.textContent = req;
                queueList.appendChild(li);
            });
        }
    }
});

connection.on("ElevatorAssigned", (data) => {
    appendLogRow(data.originFloor, data.destinationFloor, `Assigned`, data.elevatorId);

    const elevatorCard = document.getElementById(`elevator-${data.elevatorId}`);
    if (elevatorCard) {
        elevatorCard.classList.add("pulse-highlight");
        setTimeout(() => elevatorCard.classList.remove("pulse-highlight"), 2500);
    }
});

async function start() {
    try {
        await connection.start();
        console.log("SignalR Connected.");
    } catch (err) {
        console.log(err);
        setTimeout(start, 5000);
    }
}

connection.onclose(async () => {
    await start();
});

start();
