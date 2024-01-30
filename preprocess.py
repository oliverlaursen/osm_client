import xml.etree.ElementTree as ET
import json
import math
import utm

highway_blacklist = [
    "pedestrian",
    "track",
    "bus_guideway",
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
    "proposed"
]


def preprocess_osm_data(file_path, output_file):
    # Parse the XML file
    tree = ET.parse(file_path)
    root = tree.getroot()
    first_node = root.find('node')
    zero_lat = float(first_node.get('lat'))
    zero_lon = float(first_node.get('lon'))

    nodes = []
    ways = []

    # Process nodes
    for node in root.findall('node'):
        node_id = node.get('id')
        lat = float(node.get('lat'))
        lon = float(node.get('lon'))
        x, y = utm.from_latlon(lat, lon)[0:2]
        zero_x, zero_y = utm.from_latlon(zero_lat, zero_lon)[0:2]
        x = x - zero_x
        y = y - zero_y
        nodes.append({'id': node_id, 'x': x, 'y': y})

    # Process ways
    for way in root.findall('way'):
        # Ensure way is highway
        highway_tag = way.find('tag[@k="highway"]')
        if highway_tag is None or highway_tag.get('v') in highway_blacklist:
            continue
        way_id = way.get('id')
        nd_refs = [nd.get('ref') for nd in way.findall('nd')]
        ways.append({'id': way_id, 'node_refs': nd_refs})

    # Combined data
    simplified_data = {'nodes': nodes, 'ways': ways}

    # Write the simplified data to a JSON file  
    with open(output_file, 'w') as outfile:
        json.dump(simplified_data, outfile, indent=4)

    print(f"Data preprocessed and saved to {output_file}")

if __name__ == '__main__':
    name = "samsø2"
    file_path = f'maps/{name}.osm'  # Replace with your OSM data file path
    output_path = f'OSM_Unity_Client/Assets/maps/{name}.json'  # Replace with your output file path
    preprocessed_data = preprocess_osm_data(file_path, output_path)
