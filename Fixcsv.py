import csv
import sys
import re

def fix_quotes(field):
    return re.sub(r'\"', '""', field)

if len(sys.argv) < 2:
    print("Usage: python fixcsv.py <path_to_file.csv>")
    sys.exit(1)

input_file_path = sys.argv[1]
output_file_path = input_file_path.rsplit('.', 1)[0] + '_corrected.csv'

with open(input_file_path, 'r', newline='', encoding='utf-8') as infile, \
     open(output_file_path, 'w', newline='', encoding='utf-8') as outfile:

    reader = csv.reader(infile)
    writer = csv.writer(outfile)

    for row in reader:
        corrected_row = []
        for field in row:
            corrected_field = fix_quotes(field)
            corrected_row.append(corrected_field)
        
        writer.writerow(corrected_row)

print(f"Corrected CSV saved as {output_file_path}")
