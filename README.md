# OSM client bachelor project
This was our bachelor project about "Efficient Shortest Path Finding using Open Street Map Data".
Rust was used to preprocess OSM data and C# was used to implement search algorithms and Unity was used for visualisation.

Implemented algorithms:
Dijkstra, Bidirectional Dijkstra, A* (Haversine), Bidirectional A* (Haversine), A* (Landmarks), A* (Dynamic Landmarks)



![](https://github.com/oliverlaursen/osm_client/assets/43318657/1f87bc9f-060f-4745-ad67-e3266edfeb46)


https://github.com/oliverlaursen/osm_client/assets/43318657/d61c879f-42ba-4ef0-a03b-e8c223fec34b



https://github.com/oliverlaursen/osm_client/assets/43318657/e6e08f23-8df7-4bfd-96ab-1a1170069c1e



https://github.com/oliverlaursen/osm_client/assets/43318657/e551957d-4cdb-4e72-a5f4-9dfbbe377bb6




https://github.com/oliverlaursen/osm_client/assets/43318657/efaafe05-1bd9-4450-af8f-55754966a211



https://github.com/oliverlaursen/osm_client/assets/43318657/7bf4614f-f66a-4926-a164-abbfdae9b906



## How to preprocess
To preprocess a map, download an OSM.pbf map from Geofabrik and change main in main.rs to point to it, then run cargo run --release
This will output a .graph file at OSM_Unity_Client/Assets/Maps

## How to use program
To open the program that uses the .graph files, open the Unity project with root in OSM_Unity_Client. From here open Sample_Scene

