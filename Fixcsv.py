import csv
import sys
import re

if len(sys.argv) < 2:
    print("Usage: python fixcsv.py <path_to_file.csv>")
    sys.exit(1)

input_file_path = sys.argv[1]
output_file_path = input_file_path.rsplit('.', 1)[0] + '_corrected.csv'

def fix_quotes(field):
    # Escape quotes that are within the field but not surrounding it
    field = re.sub(r'(?<!^)"(?!$)', '""', field)
    return field

with open(input_file_path, 'r', newline='', encoding='utf-8') as infile, \
        open(output_file_path, 'w', newline='', encoding='utf-8') as outfile:
    reader = csv.reader(infile)
    writer = csv.writer(outfile, quoting=csv.QUOTE_MINIMAL)
    
    for row in reader:
        new_row = [fix_quotes(field) for field in row]
        writer.writerow(new_row)

print(f"Corrected CSV saved as {output_file_path}")
