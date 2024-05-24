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
    pub nodes: HashMap<NodeId, Coord>,
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
    pub distances: Vec<f32>,
    pub bi_distances: Vec<f32>,
}

#[derive(Serialize,Debug)]
pub struct NodeWriteFormat {
    pub node_id: NodeId,
    pub x: f32,
    pub y: f32,
    pub lat: f64,
    pub lon: f64,
    pub neighbours: Vec<(NodeId, f32)>,
    pub bi_neighbours: Vec<(NodeId, f32)>,
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
        println!("Size of graph: {}", graph.len());
        self.roads = Vec::new(); // Clear the roads since we don't need them anymore
        println!("Time to build graph: {:?}", time.elapsed());
        let time = std::time::Instant::now();
        Graph::minimize_graph(&mut graph, true);
        println!("Time to minimize graph: {:?}", time.elapsed());

        Preprocessor::rewrite_ids(&mut self.nodes, &mut graph);

        let bi_graph = Graph::get_bidirectional_graph(&graph);
        //let mut landmarks = Graph::random_landmarks(&graph, &bi_graph, 16);
        let mut landmarks = Graph::farthest_landmarks(&graph, &bi_graph, 16);
        landmarks.sort_by(|a, b| a.node_id.cmp(&b.node_id));

        (graph, bi_graph, landmarks.to_vec())
    }

    pub fn rewrite_ids(nodes: &mut HashMap<NodeId, Coord>, graph: &mut HashMap<NodeId, Vec<Edge>>) {
        let mut new_id = 0;
        let mut old_to_new: HashMap<NodeId, NodeId> = HashMap::new();
    
        let mut new_graph = HashMap::new();
        let mut sorted_nodes: Vec<NodeId> = graph.keys().cloned().collect();
        sorted_nodes.sort();
        for node in &sorted_nodes {
            let edges = graph.get_mut(node).unwrap();
            let mut new_edges = Vec::new();
            if !old_to_new.contains_key(node) {
                old_to_new.insert(*node, NodeId(new_id));
                new_id += 1;
            }
            for edge in edges.iter_mut() {
                if !old_to_new.contains_key(&edge.node) {
                    old_to_new.insert(edge.node, NodeId(new_id));
                    new_id += 1;
                }
    
                new_edges.push(Edge {
                    node: old_to_new[&edge.node],
                    cost: edge.cost,
                });
            }
            new_graph.insert(old_to_new[node], new_edges);
        }
        *graph = new_graph;
    
        let mut new_nodes = HashMap::new();
        let mut sorted_nodes: Vec<&NodeId> = nodes.keys().collect();
        sorted_nodes.sort();
        for node in sorted_nodes {
            let coord = nodes.get(node).unwrap();
            if !old_to_new.contains_key(node) {
                old_to_new.insert(*node, NodeId(new_id));
                new_id += 1;
            }
            new_nodes.insert(old_to_new[node], coord.clone());
        }
        *nodes = new_nodes;
    }

    pub fn build_full_graph(
        &self,
        graph: &HashMap<NodeId, Vec<Edge>>,
        bi_graph: &HashMap<NodeId, Vec<Edge>>,
        landmarks: Vec<Landmark>,
        projected_points: &HashMap<NodeId, (f32, f32)>,
    ) -> FullGraph {
        let mut nodes: Vec<NodeWriteFormat> = graph
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
                    lat: node.lat,
                    lon: node.lon,
                    neighbours,
                    bi_neighbours,
                }
            })
            .collect();
        nodes.sort_by(|a, b| a.node_id.cmp(&b.node_id));
        FullGraph { nodes, landmarks }
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
                    self.roads.push(Road {
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

        self.get_nodes(filename, &nodes_to_keep_hashset);
    }

    pub fn get_nodes(&mut self, filename: &str, nodes_to_keep: &HashSet<NodeId>)  {
        let r = std::fs::File::open(&std::path::Path::new(filename)).unwrap();
        let mut pbf = osmpbfreader::OsmPbfReader::new(r);
        for obj in pbf.par_iter().map(Result::unwrap) {
            match obj {
                osmpbfreader::OsmObj::Node(node) if nodes_to_keep.contains(&node.id) => {
                    self.nodes.insert(node.id, Coord { lat: node.lat(), lon: node.lon() });
                }
                osmpbfreader::OsmObj::Node(_) => continue,
                _ => break, // Can return early since nodes are at the start of the file
            }
        }
    }

    pub fn new() -> Self {
        Preprocessor {
            nodes: HashMap::new(),
            roads: Vec::new(),
        }
    }

    pub fn project_nodes_to_2d(&self) -> HashMap<NodeId, (f32, f32)> {
        let center_point = self.nodes.iter().fold((0.0, 0.0), |acc, (_, node)| {
            (acc.0 + node.lat, acc.1 + node.lon)
        });
        let center_point = (
            center_point.0 / self.nodes.len() as f64,
            center_point.1 / self.nodes.len() as f64,
        );

        let projected_points = self
            .nodes
            .par_iter()
            .map(|(nodeid, node)| {
                let (x, y) = azimuthal_equidistant_projection(*node, center_point);
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
