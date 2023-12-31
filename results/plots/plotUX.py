import pandas as pd
import seaborn as sns
import matplotlib.pyplot as plt
import numpy as np

# Load your CSV file into a pandas DataFrame
df = pd.read_csv('../data/uxResults.csv', sep=';', header=None)

#df = df.transpose()
# Assuming the first row contains the headers for each metric
headers = df.iloc[0]

# Create a new DataFrame without the header row
df = df[1:]

# Set the headers for each column
df.columns = headers

# Convert the data to numeric values
df = df.apply(pd.to_numeric, errors='coerce')

# Create a list of metrics for box plots
metrics = list(df.columns)

# Set the figure size
#plt.figure(figsize=(12, 8))

print(headers)
#print(headers[0])
x = [1,2,3,4,5]
min_val = 1
max_val = 5
bin_width = 0.5

colors = ['#66c2a5','#fc8d62','#8da0cb','#e78ac3','#a6d854','#ffd92f','#e5c494','#b3b3b3','#8dd3c7','#bebada']

# Loop through each metric and create a box plot
for i, metric in enumerate(metrics):
    print(metric)
    plt.figure(figsize=(10, 6))
    #plt.subplot(2, 5, metrics.index(metric) + 1)  # Adjust the subplot grid based on the number of metrics
    #sns.boxplot(x=df[metric], y=df.index + 1)    # Add 1 to the index for the y-axis
    #bin_edges = [val - 0.5 for val in sorted(set(df[metric]))] + [max(data[-1] + 0.5, max(data) + 0.5)]
    sns.histplot(data=df[metric] ,discrete=True,kde=False,palette='pastel',color=colors[i])
    #plt.xticks(np.arange(min_val-bin_width/2, max_val+bin_width/2, bin_width))
    plt.xticks(x)
    plt.xlim(0, 6)
    plt.title(metric)
    plt.xlabel('Rating (1 - Strongly Disagree, 5 - Strongly Agree)')
    plt.ylabel('Count')

    plt.savefig(str(i) + '.png')


# # Adjust layout for better visualization
# plt.tight_layout()

# # Show the plots
plt.savefig("plots.png")
