import xml.etree.ElementTree as ET
import json

def preprocess_osm_data(file_path, output_file):
    # Parse the XML file
    tree = ET.parse(file_path)
    root = tree.getroot()

    nodes = {}
    ways = {}

    # Process nodes
    for node in root.findall('node'):
        node_id = node.get('id')
        lat = float(node.get('lat'))
        lon = float(node.get('lon'))
        nodes[node_id] = lat,lon

    # Process ways
    for way in root.findall('way'):
        way_id = way.get('id')
        nd_refs = [nd.get('ref') for nd in way.findall('nd')]
        ways[way_id] = {'node_refs': nd_refs}

    # Combined data
    simplified_data = {'nodes': nodes, 'ways': ways}

    # Write the simplified data to a JSON file
    with open(output_file, 'w') as outfile:
        json.dump(simplified_data, outfile, indent=4)

    print(f"Data preprocessed and saved to {output_file}")

if __name__ == '__main__':
    file_path = 'maps/tunø.osm'  # Replace with your OSM data file path
    output_path = 'maps/tunø.json'  # Replace with your output file path
    preprocessed_data = preprocess_osm_data(file_path, output_path)
