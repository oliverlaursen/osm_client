use std::cmp::Ordering;
use osmpbfreader::NodeId;
use serde::Serialize;

#[derive(Clone, Debug, Eq, PartialEq, Serialize)]
pub struct Edge {
    pub node: NodeId,
    pub cost: u32, // This could be distance, time, etc.
}

impl Ord for Edge {
    fn cmp(&self, other: &Self) -> Ordering {
        // Notice we flip the ordering here because BinaryHeap is a max heap by default
        other.cost.cmp(&self.cost)
    }
}

impl PartialOrd for Edge {
    fn partial_cmp(&self, other: &Self) -> Option<Ordering> {
        Some(self.cmp(other))
    }
}