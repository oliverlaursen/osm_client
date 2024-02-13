use crate::preprocessor::coord::Coord;

use crate::{azimuthal_equidistant_projection, FullGraph};
use osmpbfreader::{NodeId, WayId};
use rayon::iter::{FromParallelIterator, IntoParallelRefIterator, ParallelIterator};
use serde::Serialize;
use std::ops::Add;
use std::{
    collections::{HashMap, HashSet},
    io::Write,
};

#[derive(Debug, Clone, Serialize)]
pub struct Node {
    pub id: NodeId,
    pub coord: Coord,
}

#[derive(Debug, Clone)]
pub struct Road {
    pub id: WayId,
    pub node_refs: Vec<NodeId>,
    pub direction: CarDirection,
}

#[derive(Debug, PartialEq, Clone)]
pub enum CarDirection {
    FORWARD,
    TWOWAY,
}

#[derive(Clone)]
pub struct Preprocessor {
    pub nodes_to_keep: HashSet<NodeId>,
    pub nodes: HashMap<NodeId, Node>,
    pub roads: Vec<Road>,
}

fn create_blacklist() -> HashSet<&'static str> {
    let blacklist: HashSet<&str> = HashSet::from_iter([
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
    ]);
    blacklist
}

impl Preprocessor {
    pub fn is_valid_highway(&self, blacklist: &HashSet<&str>, tags: &osmpbfreader::Tags) -> bool {
        tags.iter()
            .any(|(k, v)| (k == "highway" && !blacklist.contains(v.as_str())))
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
                    roads.push(Road {
                        id: way.id,
                        node_refs: way.nodes,
                        direction: way.tags.get("oneway").map_or(CarDirection::TWOWAY, |v| {
                            if v == "yes" {
                                CarDirection::FORWARD
                            } else {
                                CarDirection::TWOWAY
                            }
                        }),
                    })
                }
                _ => continue,
            }
        }
        let nodes_to_keep_hashset = HashSet::from_par_iter(nodes_to_keep);

        let nodes = Self::get_nodes(filename, &nodes_to_keep_hashset);
        let nodes_hashmap: HashMap<NodeId, Node> = nodes
            .par_iter()
            .map(|node| (node.id, node.clone()))
            .collect();
        self.nodes_to_keep = nodes_to_keep_hashset;
        self.nodes = nodes_hashmap;
        self.roads = roads;
    }

    pub fn get_nodes(filename: &str, nodes_to_keep: &HashSet<NodeId>) -> Vec<Node> {
        let mut nodes: Vec<Node> = Vec::new();
        let r = std::fs::File::open(&std::path::Path::new(filename)).unwrap();
        let mut pbf = osmpbfreader::OsmPbfReader::new(r);
        for obj in pbf.par_iter().map(Result::unwrap) {
            match obj {
                osmpbfreader::OsmObj::Node(node) => {
                    if !nodes_to_keep.contains(&node.id) {
                        continue;
                    }
                    nodes.push(Node {
                        id: node.id,
                        coord: Coord {
                            lat: node.lat(),
                            lon: node.lon(),
                        },
                    })
                }
                _ => return nodes, // Can return early since nodes are at the start of the file
            }
        }
        return nodes;
    }

    pub fn filter_nodes(&mut self) {
        // Filter out nodes that are not in nodes_to_keep
        let nodes = self
            .nodes
            .par_iter() // Use a parallel iterator
            .map(|(nodeid, node)| (*nodeid, node.clone())) // Dereference the tuple elements before cloning
            .filter(|(nodeid, _)| self.nodes_to_keep.contains(nodeid))
            .collect();
        self.nodes = nodes;
    }

    pub fn new() -> Self {
        Preprocessor {
            nodes_to_keep: HashSet::new(),
            nodes: HashMap::new(),
            roads: Vec::new(),
        }
    }

    pub fn write_full_graph(graph: FullGraph, filename: &str) {
        /*
           Format:
           nodeId x y neighbour cost neighbour cost \n
        */
        println!("Writing to file: {}, {}", filename, graph.nodes.len());
        let mut output = String::new();
        graph.nodes.iter().for_each(|(node_id, (x,y))|{ 
            let edges_option = graph.graph.get(node_id);
            let edges = match edges_option {
                Some(edges) => edges,
                None => return
            };
            let mut node_string = format!("{} {} {}", node_id.0, x, y); //nodeId x y
            for edge in edges {
                node_string.push_str(&format!(" {} {}", edge.node.0, edge.cost));
            }
            output.push_str(&(node_string + "\n"));
        }); 
        
        let mut file = std::fs::File::create(filename).unwrap();
        file.write_all(output.as_bytes()).unwrap();
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

    pub fn remove_nodes(&mut self, new_nodes: HashSet<NodeId>) {
        for node_id in new_nodes {
            self.nodes.remove(&node_id);
        }
    }
}

//TESTS
fn initialize(filename: &str) -> Preprocessor {
    let mut preprocessor = Preprocessor::new();
    preprocessor.get_roads_and_nodes(filename);
    preprocessor.filter_nodes();
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
    assert_eq!(CarDirection::FORWARD, preprocessor.roads[0].direction);
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

#[test]
fn all_nodes_to_keep_are_kept() {
    //checks that all nodes in nodes kept is also in nodes to keep.
    let preprocessor = initialize("src/test_data/minimal.osm.testpbf");
    let nodes_to_keep = &preprocessor.nodes_to_keep;
    let nodes_kept = &preprocessor.nodes;
    for node in nodes_kept {
        assert_eq!(true, nodes_to_keep.contains(node.0));
    }
}
