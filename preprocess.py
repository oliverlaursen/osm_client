import xml.etree.ElementTree as ET
import json

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
        lat = float(node.get('lat')) - zero_lat
        lon = float(node.get('lon')) - zero_lon
        nodes.append({'id':node_id, 'lat':lat, 'lon':lon})

    # Process ways
    for way in root.findall('way'):
        # Ensure way is highway
        if way.find('tag[@k="highway"]') is None:
            continue
        way_id = way.get('id')
        nd_refs = [nd.get('ref') for nd in way.findall('nd')]
        ways.append({'id':way_id, 'node_refs': nd_refs})

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
