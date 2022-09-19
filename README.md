# ChroMapTogether
Matchmaking server for [ChroMapper](https://github.com/Caeden117/ChroMapper) United Mapping.

## FAQ

### What is ChroMapTogether?
ChroMapTogether is just a back-end ASP.NET Core server which simplfies the process of hosting and joining ChroMapper United Mapping sessions. This is achieved by generating short room codes which are tied to the Host's IP and port.

### What is ChroMapper United Mapping?
ChroMapper United Mapping (abbreviated as C.U.M.) is the multiplayer protocol which allows multiple ChroMapper clients to connect and work together on a Beat Saber map. To put it simply: Multi-player ChroMapper.

### What is ChroMapper?
[ChroMapper](https://github.com/Caeden117/ChroMapper) is an open source beatmap editor for the VR rhythm game *Beat Saber*. The repository has lots more information.

### Is ChroMapTogether required?
ChroMapTogether uses a trick called [UDP Hole Punching](https://en.wikipedia.org/wiki/UDP_hole_punching) to circumvent the need to port forward, which not a lot of aspiring mappers can do at home.

Users can still choose to directly connect to a host IP and port, provided the host has port forwarded (or another alternative is used). ChroMapper United Mapping by itself is Peer to Peer; ChroMapTogether just makes it easier to host or join a C.U.M. session.

## Hosting
I already host a ChroMapTogether server, and it's the default server for all ChroMapper users. However, if you wish to host a separate ChroMapTogether server, you can follow the instructions below to set up a custom server.

### Usage (Docker)
[Docker](https://www.docker.com/get-started) is recommended as it automates the process of building and launching the server.

#### Compose
A Docker Compose file is provided in the repository. You can use this directly, but it is recommended to build upon this file to suit your own needs.

1. Download `docker-compose.yml` from the root of the repository.
2. Make any edits you want to change, it's not necessary to use the file as-is.
3. Run the `docker-compose up` command.

#### Manual
1. Build the image (`docker build ghcr.io/caeden117/chromaptogether:master`)
2. Start the container (`docker run --name ChroMapTogether ghcr.io/caeden117/chromaptogether:master`)

### Usage (.NET CLI)
Ensure that you have the [.NET 5.0 SDK](https://dotnet.microsoft.com/download) installed

1. Clone the repo
2. Restore NuGet packages: `dotnet restore ChroMapTogether.csproj`
3. Build the project: `dotnet publish ChroMapTogether.csproj -c Release`
4. Navigate to the output folder and run the server: `dotnet ChroMapTogether.dll`

## Custom Servers
To create entirely custom servers, view the API specification in the `ChroMapTogether/api` folder. Good luck!

## Credits
Massive thanks to [@Goobwabber](https://github.com/Goobwabber) for helping me design ChroMapper United Mapping, and for providing the base to ChroMapTogether. None of this would've happened without her pushing me in the right directions and motivating me to completely pursue multi-player functionality in ChroMapper.
