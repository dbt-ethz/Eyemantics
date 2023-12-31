import seaborn as sns
import matplotlib.pyplot as plt
import numpy as np

# Sample data (replace this with your actual data)
data1 = {
    'CPU': [26.06633,27.54976,29.16865,28.26687,27.75128,28.66858,27.88275,27.84848,28.90387,27.76224]
}

data2 = {
    'GPU': [2.504524,1.835281,1.751038,1.566879,1.584244,1.650635,1.617355,2.017334,1.634598,1.817947]
}

labels = ['CPU', 'GPU']

# Set Seaborn style and color palette
sns.set(style="whitegrid", palette="pastel")

# Create subplots
fig, axes = plt.subplots(nrows=1, ncols=2, figsize=(11, 8), sharex=True)

# Plot for data1
ax1 = sns.boxplot(data=list(data1.values()),ax=axes[0])
axes[0].set_ylabel('Time [s]', fontsize=15)
axes[0].set_xlabel('CPU', fontsize=15)
# axes[0].set_title('Box Plot of Measure 1 Over 10 Runs', fontsize=14)

# Annotate mean values above the mean line for data1
for i, mean_val in enumerate([np.median(np.array(run_data)) for run_data in data1.values()]):
    ax1.text(i, mean_val + 0.1, f"{mean_val:.2f}", ha='center', va='bottom', color='black', fontsize=15)

# Plot for data2
ax2 = sns.boxplot(data=list(data2.values()), ax=axes[1])
axes[1].set_ylabel('Time [s]', fontsize=15)
# axes[1].set_title('Box Plot of Measure 2 Over 10 Runs', fontsize=14)
axes[1].set_xlabel('GPU', fontsize=15)

# Annotate mean values above the mean line #for data2
#med = np.median(np.array(run_data))
for i, mean_val in enumerate([np.median(np.array(run_data)) for run_data in data2.values()]):
    ax2.text(i, mean_val + 0.03, f"{mean_val:.2f}", ha='center', va='bottom', color='black', fontsize=15)

# Set x-axis ticks and labels
#plt.xticks(range(2),labels, fontsize=10)

# Add a single title for the entire figure
plt.suptitle('Runtime Measurements over 10 runs', fontsize=24)

# Adjust layout
plt.tight_layout()

# Save the plot as an image file (adjust the filename and format as needed)
plt.savefig('boxplot_measure_over_two_subplots_with_means.png')

# Display the plot (optional, comment this line if you don't want to display the plot in the notebook)
#plt.show()

