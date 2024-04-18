use crate::preprocessor::coord::Coord;

use crate::{azimuthal_equidistant_projection, Graph};
use osmpbfreader::NodeId;
use rayon::iter::{FromParallelIterator, IntoParallelRefIterator, ParallelIterator};
use serde::Serialize;
use std::collections::{HashMap, HashSet};

use rmp_serde::Serializer;

use super::edge::Edge;

#[derive(Debug, Clone, Serialize)]
pub struct Node {
    pub id: NodeId,
    pub coord: Coord,
}

#[derive(Debug, Clone)]
pub struct Road {
    pub node_refs: Vec<NodeId>,
    pub direction: CarDirection,
}

#[derive(Debug, PartialEq, Eq, Clone)]
pub enum CarDirection {
    Forward,
    Twoway,
}

#[derive(Clone)]
pub struct Preprocessor {
    pub nodes_to_keep: HashSet<NodeId>,
    pub nodes: HashMap<NodeId, Node>,
    pub roads: Vec<Road>,
}

#[derive(Serialize)]
pub struct FullGraph {
    pub nodes: Vec<NodeWriteFormat>,
    pub landmarks: Vec<Landmark>,
}

#[derive(Serialize, Clone)]
pub struct Landmark {
    pub node_id: NodeId,
    pub distances: HashMap<NodeId, u32>,
    pub bi_distances: HashMap<NodeId, u32>
}

#[derive(Serialize)]
pub struct NodeWriteFormat {
    pub node_id: NodeId,
    pub x: f32,
    pub y: f32,
    pub lat: f64,
    pub lon: f64,
    pub neighbours: Vec<(NodeId, u32)>,
    pub bi_neighbours: Vec<(NodeId, u32)>,
}

fn create_blacklist() -> HashSet<&'static str> {
    HashSet::from_iter([
        "pedestrian",
        "footway",
        "steps",
        "path",
        "cycleway",
        "proposed",
        "construction",
        "bridleway",
        "abandoned",
        "platform",
        "raceway",
        "service",
        "services",
        "rest_area",
        "escape",
        "raceway",
        "busway",
        "footway",
        "bridlway",
        "steps",
        "corridor",
        "via_ferreta",
        "sidewalk",
        "crossing",
        "proposed",
        "track",
    ])
}

impl Preprocessor {
    pub fn is_valid_highway(&self, blacklist: &HashSet<&str>, tags: &osmpbfreader::Tags) -> bool {
        tags.iter()
            .any(|(k, v)| (k == "highway" && !blacklist.contains(v.as_str())))
            && !tags.contains_key("area")
    }

    pub fn build_graph(
        &mut self,
    ) -> (
        HashMap<NodeId, Vec<Edge>>,
        HashMap<NodeId, Vec<Edge>>,
        Vec<Landmark>,
    ) {
        let time = std::time::Instant::now();
        let mut graph = Graph::build_graph(&self.nodes, &self.roads);
        self.roads = Vec::new(); // Clear the roads since we don't need them anymore
        println!("Time to build graph: {:?}", time.elapsed());
        let time = std::time::Instant::now();
        Graph::minimize_graph(&mut graph, true);
        println!("Time to minimize graph: {:?}", time.elapsed());
        
        let bi_graph = Graph::get_bidirectional_graph(&mut graph);
        //let landmark_nodes = Graph::get_random_nodes(&graph, 16);
        //let landmarks = Graph::add_landmarks(&graph,&bi_graph, landmark_nodes);
        let landmarks = Graph::farthest_nodes(&graph, &bi_graph, 16);

        (graph, bi_graph, landmarks.to_vec())
    }

    pub fn build_full_graph(
        &self,
        graph: &HashMap<NodeId, Vec<Edge>>,
        bi_graph: &HashMap<NodeId, Vec<Edge>>,
        landmarks: Vec<Landmark>,
        projected_points: &HashMap<NodeId, (f32, f32)>,
    ) -> FullGraph {
        FullGraph {
            nodes: graph
                .iter()
                .map(|(node_id, edges)| {
                    let (x, y) = projected_points.get(node_id).unwrap();
                    let neighbours = edges.iter().map(|edge| (edge.node, edge.cost)).collect();
                    let bi_neighbours = bi_graph
                        .get(node_id)
                        .unwrap()
                        .iter()
                        .map(|edge| (edge.node, edge.cost))
                        .collect();

                    let node = self.nodes.get(node_id).unwrap();
                    NodeWriteFormat {
                        node_id: *node_id,
                        x: *x,
                        y: *y,
                        lat: node.coord.lat,
                        lon: node.coord.lon,
                        neighbours,
                        bi_neighbours,
                    }
                })
                .collect(),
            landmarks,
        }
    }

    pub fn write_graph(full_graph: FullGraph, filename: &str) {
        /*
           Format:
           nodeId x y neighbour cost neighbour cost \n
        */
        let filename = "../OSM_Unity_Client/Assets/Maps/".to_owned() + filename;

        let mut buf = Vec::new();
        full_graph
            .serialize(&mut Serializer::new(&mut buf))
            .unwrap();
        std::fs::write(filename, buf).unwrap();
    }

    pub fn get_roads_and_nodes(&mut self, filename: &str) {
        let r = std::fs::File::open(&std::path::Path::new(filename)).unwrap();
        let mut pbf = osmpbfreader::OsmPbfReader::new(r);
        let mut roads: Vec<Road> = Vec::new();
        let mut nodes_to_keep: Vec<NodeId> = Vec::new();
        let blacklist = create_blacklist();
        for obj in pbf.par_iter().map(Result::unwrap) {
            match obj {
                osmpbfreader::OsmObj::Way(way) => {
                    if !self.is_valid_highway(&blacklist, &way.tags) {
                        continue;
                    }
                    nodes_to_keep.extend(&way.nodes);
                    let oneway = way.tags.get("oneway").map_or(false, |v| v == "yes");
                    let roundabout = way.tags.values().any(|v| v == "roundabout");
                    roads.push(Road {
                        node_refs: way.nodes,
                        direction: if oneway || roundabout {
                            CarDirection::Forward
                        } else {
                            CarDirection::Twoway
                        },
                    })
                }
                _ => continue,
            }
        }
        let nodes_to_keep_hashset = HashSet::from_par_iter(nodes_to_keep);

        let nodes = self.get_nodes(filename, &nodes_to_keep_hashset);
        let nodes_hashmap: HashMap<NodeId, Node> = nodes
            .par_iter()
            .map(|node| (node.id, node.clone()))
            .collect();
        self.nodes_to_keep = nodes_to_keep_hashset;
        self.nodes = nodes_hashmap;
        self.roads = roads;
    }

    pub fn get_nodes(&mut self, filename: &str, nodes_to_keep: &HashSet<NodeId>) -> Vec<Node> {
        let mut nodes: Vec<Node> = Vec::new();
        let r = std::fs::File::open(&std::path::Path::new(filename)).unwrap();
        let mut pbf = osmpbfreader::OsmPbfReader::new(r);
        for obj in pbf.par_iter().map(Result::unwrap) {
            match obj {
                osmpbfreader::OsmObj::Node(node) if nodes_to_keep.contains(&node.id) => {
                    nodes.push(Node {
                        id: node.id,
                        coord: Coord {
                            lat: node.lat(),
                            lon: node.lon(),
                        },
                    })
                }
                osmpbfreader::OsmObj::Node(_) => continue,
                _ => break, // Can return early since nodes are at the start of the file
            }
        }
        nodes
    }

    pub fn new() -> Self {
        Preprocessor {
            nodes_to_keep: HashSet::new(),
            nodes: HashMap::new(),
            roads: Vec::new(),
        }
    }

    pub fn project_nodes_to_2d(&self) -> HashMap<NodeId, (f32, f32)> {
        let center_point = self.nodes.iter().fold((0.0, 0.0), |acc, (_, node)| {
            (acc.0 + node.coord.lat, acc.1 + node.coord.lon)
        });
        let center_point = (
            center_point.0 / self.nodes.len() as f64,
            center_point.1 / self.nodes.len() as f64,
        );

        let projected_points = self
            .nodes
            .par_iter()
            .map(|(nodeid, node)| {
                let (x, y) = azimuthal_equidistant_projection(node.coord, center_point);
                (*nodeid, (x as f32, y as f32))
            })
            .collect();
        projected_points
    }
}

//TESTS
fn initialize(filename: &str) -> Preprocessor {
    let mut preprocessor = Preprocessor::new();
    preprocessor.get_roads_and_nodes(filename);
    preprocessor
}

#[test]
fn test_real_all() {
    //checks if file has been parsed correctly, with 2 nodes and 1 road
    let preprocessor = initialize("src/test_data/minimal.osm.testpbf");
    assert_eq!(1, preprocessor.roads.len());
    assert_eq!(2, preprocessor.nodes.len());
}

#[test]
fn road_is_oneway() {
    //checks if road is a oneway road
    let preprocessor = initialize("src/test_data/minimal.osm.testpbf");
    assert_eq!(CarDirection::Forward, preprocessor.roads[0].direction);
}

#[test]
fn does_not_include_blacklisted_roads() {
    //length of the road list should be 1, since one of the roads is pedestrian, which is blacklisted
    let preprocessor = initialize("src/test_data/minimal_ignored_road.osm.testpbf");
    assert_eq!(1, preprocessor.roads.len());
}

#[test]
fn one_node_is_dropped() {
    //amount of nodes kept are 2, because one node is not referenced by a road
    let preprocessor = initialize("src/test_data/one_node_is_dropped.osm.testpbf");
    assert_eq!(2, preprocessor.nodes.len());
}
