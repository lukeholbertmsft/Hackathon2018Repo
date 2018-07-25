import matplotlib.pyplot as plt
import numpy as np
import pylab
import csv
import os
from os import listdir

inDir = "..\\Clean Kepler Data\\Clean Confirmed Positive"
outDir = "..\\ImageFiles\\PositiveImages"

for name in listdir(inDir):

    inPath = inDir + "\\" + name
    outPath = outDir + "\\" + name.replace(".tbl", ".png")
    file = open(inPath, "r")

    time = []
    flux = []

    reader = csv.reader(file)
    data = list(list(rec) for rec in csv.reader(file,delimiter=','))

    for row in data:
        if('null' not in row[0] and 'null' not in row[1] and 'TIME' not in row[0]):	
            time.append(float(row[0]))
            flux.append(float(row[1]))

    plt.scatter(time,flux,marker='+')
    plt.axis('off')
    plt.savefig(outPath,dpi=300,bbox_inches='tight')
    plt.clf()
