\# 👨‍🍳 Multiplayer Cookout!



> \[cite\_start]\*\*Graduation Thesis Project - Solo Developer\*\*  | \[cite\_start]\*\*Grade: 8.0/10\*\* \[cite: 21]



A frantic, server-authoritative multiplayer cooking game built with Unity. This project demonstrates advanced networking concepts, client-side prediction, and seamless integration with Unity Gaming Services.



!\[Gameplay Demo](Link\_To\_Your\_Gameplay\_Gif\_Or\_YouTube\_Video\_Here)



\## 🌟 Key Features



\* \[cite\_start]\*\*Server-Authoritative Networking:\*\* Robust multiplayer architecture utilizing Netcode for GameObjects (NGO)\[cite: 16].

\* \[cite\_start]\*\*State Synchronization:\*\* Handles real-time state sync for up to 4 players with client-side prediction to effectively minimize network latency\[cite: 16].

\* \[cite\_start]\*\*Cloud Infrastructure:\*\* Integrated Unity Gaming Services (Relay, Lobby) for seamless, port-forward-free matchmaking\[cite: 17].

\* \[cite\_start]\*\*Player Economy \& Data:\*\* Implemented Cloud Save and Authentication to securely manage player cosmetics, hats, and session gold\[cite: 18].

\* \[cite\_start]\*\*Smart Kitchen AI:\*\* Designed autonomous bot players using NavMesh and Finite State Machine (FSM) for dynamic task prioritization when human players are unavailable\[cite: 19].

\* \[cite\_start]\*\*Multiple Game Modes:\*\* Features 3 distinct modes including Co-op, PvP (2 teams), and a complex PvP 3-Team mode with custom scoring algorithms\[cite: 20].



\## 🛠️ Technology Stack



\* \*\*Game Engine:\*\* Unity 3D (C#)

\* \*\*Networking:\*\* Netcode for GameObjects (NGO), Unity Transport

\* \*\*Backend Services:\*\* Unity Authentication, Lobby, Relay, Cloud Save

\* \*\*Core Patterns:\*\* Singleton, Observer, Finite State Machine (FSM)



\## 🚀 Getting Started



\### Prerequisites

\* Unity Editor (Version 2022.3 LTS or higher recommended).

\* A Unity Dashboard account to link Unity Gaming Services.



\### Installation

1\. Clone the repository:

&nbsp;  `git clone https://github.com/arapat1412/Multiplayer-Cookout.git`

2\. Open the project in Unity Hub.

3\. Go to \*\*Edit > Project Settings > Services\*\* and link the project to your Unity Dashboard organization to enable Lobby and Relay services.

4\. Open the `MainMenuScene` and hit Play!



\## 💡 Technical Highlights



The game employs a strict Server-Authoritative model. All critical gameplay actions (chopping, moving items, delivering plates) are validated on the server via `ServerRpc`. To ensure a smooth player experience despite network latency, visual elements and animations utilize client-side prediction before the server confirms the final state.



\## 👤 Author



\*\*Ho Chi Minh City, Vietnam\*\*

\* GitHub: \[@arapat1412](https://github.com/arapat1412)

\* Contact: mogia1711a@gmail.com

