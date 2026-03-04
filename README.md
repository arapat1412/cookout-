👨‍🍳 Cookout! - Multiplayer Cooking Game
Graduation Thesis Project | Grade: 8.0/10

Cookout! is a multiplayer cooking game built with Unity. I developed this as my graduation thesis to dive deep into multiplayer game development, focusing on network synchronization, server-authoritative logic, and integrating cloud services.

🌟 What's in the Game?
Strict Server-Authoritative Networking: Built using Netcode for GameObjects (NGO). The server dictates all game logic and validates player actions to ensure everyone stays in sync.

Hassle-Free Matchmaking: Integrated Unity Gaming Services (Relay and Lobby) so players can host and join games easily without messing with port forwarding.

Persistent Player Data: Uses Unity Cloud Save and Authentication. Players can earn gold, buy hats, and keep their cosmetics across different sessions.

Kitchen AI (Bots): If you don't have enough friends to fill a lobby, custom NavMesh-based bots will jump in. They use a Finite State Machine to figure out what needs to be chopped, cooked, or delivered next.

3 Game Modes: * Co-op: Work together to get the highest score.

PvP (2 Teams): Classic Red vs. Blue kitchen battle.

PvP (3 Teams): A more chaotic mode requiring custom logic to handle 3 separate kitchens and scoring systems.

🛠️ Built With
Engine: Unity 3D (C#)

Multiplayer: Netcode for GameObjects (NGO), Unity Transport

Backend: Unity Gaming Services (Authentication, Lobby, Relay, Cloud Save)

🚀 How to Run It
Prerequisites
Unity Editor (Version 2022.3 LTS or higher recommended).

You'll need to link the project to your own Unity Dashboard to use the UGS features.

Setup
Clone this repo:
git clone https://github.com/arapat1412/Multiplayer-Cookout.git

Open the project in Unity Hub.

Go to Edit > Project Settings > Services and link it to your Unity organization. Make sure Lobby, Relay, Cloud Save, and Authentication are enabled.

Open the MainMenuScene and hit Play!

💡 Under the Hood
The core networking philosophy of this game is "never trust the client". Every critical action—like picking up a tomato, chopping a cabbage, or delivering a plate—is sent to the server via ServerRpc. The server validates the action, updates its own state, and then syncs the changes back to all clients. This ensures the game state remains consistent for everyone in the room, even during chaotic kitchen moments.

👤 About the Developer
Mô Ham Mách A Ra Pát | Ho Chi Minh City, Vietnam

GitHub: @arapat1412

Email: mogia1711a@gmail.com
