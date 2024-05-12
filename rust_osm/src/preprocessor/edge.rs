use osmpbfreader::NodeId;
use serde::Serialize;
use std::cmp::Ordering;
use std::hash::{Hash, Hasher};

#[derive(Clone, Debug, Serialize, Copy)]
pub struct Edge {
    pub node: NodeId,
    pub cost: f32,
}

impl Edge {
    pub fn new(node: NodeId, cost: f32) -> Self {
        Edge { node, cost }
    }
}

impl Ord for Edge {
    fn cmp(&self, other: &Self) -> Ordering {
        other
            .cost
            .partial_cmp(&self.cost)
            .unwrap_or(Ordering::Equal)
    }
}

impl PartialOrd for Edge {
    fn partial_cmp(&self, other: &Self) -> Option<Ordering> {
        other.cost.partial_cmp(&self.cost)
    }
}

impl PartialEq for Edge {
    fn eq(&self, other: &Self) -> bool {
        self.cost.to_bits() == other.cost.to_bits()
    }
}

impl Eq for Edge {}

impl Hash for Edge {
    fn hash<H: Hasher>(&self, state: &mut H) {
        self.node.hash(state);
        self.cost.to_bits().hash(state);
    }
}
