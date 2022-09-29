# ChroMapTogether
UDP relay server for [ChroMapper](https://github.com/Caeden117/ChroMapper) United Mapping.

## FAQ

### What is ChroMapTogether?
ChroMapTogether is a dead-simple ASP.NET Core server which simplfies the process of hosting and joining ChroMapper United Mapping sessions.

### What is ChroMapper United Mapping?
ChroMapper United Mapping (abbreviated as C.U.M.) is the multiplayer protocol which allows multiple ChroMapper clients to connect and work together on a Beat Saber map. To put it simply: Multi-player ChroMapper.

### What is ChroMapper?
[ChroMapper](https://github.com/Caeden117/ChroMapper) is an open source beatmap editor for the VR rhythm game *Beat Saber*. The repository has lots more information.

### Is ChroMapTogether required?
ChroMapTogether is essentially a UDP relay which circumvents the need to port forward, something not a lot of aspiring mappers can do at home.

Users can still choose to directly connect to a host IP and port, provided the host has port forwarded (or another alternative is used). ChroMapper United Mapping by itself is Peer to Peer; ChroMapTogether just makes it easier to host or join a C.U.M. session.

## Hosting
I already host a ChroMapTogether server, and it's the default server for all ChroMapper users. However, if you wish to host a separate ChroMapTogether server, you can follow the instructions below to set up a custom server.

No matter which solution you use, make sure the following ports are open:
- TCP: 80 and/or 443 (for HTTP/S requests)
- UDP: 6969

### Usage (Docker)
[Docker](https://www.docker.com/get-started) is recommended as it automates the process of building and launching the server.

1. Build the image (`docker build ghcr.io/caeden117/chromaptogether:master`)
2. Start the container (`docker run --name ChroMapTogether ghcr.io/caeden117/chromaptogether:master`)

### Usage (.NET CLI)
Ensure that you have the [.NET 6.0 SDK](https://dotnet.microsoft.com/download) installed

1. Clone the repo
2. Restore NuGet packages: `dotnet restore ChroMapTogether.csproj`
3. Build the project: `dotnet publish ChroMapTogether.csproj -c Release`
4. Navigate to the output folder and run the server: `dotnet ChroMapTogether.dll`

## Credits
Massive thanks to [@Goobwabber](https://github.com/Goobwabber) for helping me design ChroMapper United Mapping, and for providing the base to ChroMapTogether. None of this would've happened without her pushing me in the right directions and motivating me to completely pursue multi-player functionality in ChroMapper.
