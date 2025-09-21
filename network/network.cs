using System;
using System.Collections.Generic;
using System.IO;

public class Network
{
    private List<Layer> layers;
    private List<List<double>> inputsCache;

    public Network()
    {
        layers = new List<Layer>();
        inputsCache = new List<List<double>>();
    }

    // ----------------- AddLayer -----------------
    public void AddLayer(string layerType)
    {
        switch (layerType)
        {
            case "Sigmoid": layers.Add(new Sigmoid()); break;
            case "Tanh": layers.Add(new Tanh()); break;
            case "Relu": layers.Add(new Relu()); break;
            default: Console.WriteLine($"Unknown layer type {layerType}"); break;
        }
    }

    public void AddLayer(string layerType, int inputs, int outputs)
    {
        if (layerType == "Dense")
            layers.Add(new Dense(inputs, outputs));
        else
            Console.WriteLine($"Unknown layer type {layerType}");
    }

    public void AddLayerC(string layerType, int h, int w, int kh, int kw, int kernels, int stride = 1)
    {
        //Console.WriteLine($"AddLayer called: {layerType}");
        if (layerType == "Conv2D")
            layers.Add(new Conv2D(h, w, kh, kw, kernels, stride));
        else
            Console.WriteLine($"Unknown layer type {layerType}");
    }

    public void AddLayer(string layerType, int h, int w, int ph, int pw, int stride = -1)
    {
        if (layerType == "MaxPooling2D")
            layers.Add(new MaxPooling2D(h, w, ph, pw, stride));
        else
            Console.WriteLine($"Unknown layer type {layerType}");
    }

    // ----------------- Forward Pass -----------------
    public List<double> ForwardPass(List<double> input)
    {
        inputsCache.Clear();
        foreach (var layer in layers)
        {
            inputsCache.Add(new List<double>(input));
            input = layer.ForwardPass(input);
        }
        inputsCache.Add(new List<double>(input));
        return input;
    }

    // ----------------- Loss -----------------
    private double MSE(List<double> target, List<double> output)
    {
        double sum = 0;
        for (int i = 0; i < target.Count; i++)
            sum += Math.Pow(target[i] - output[i], 2);
        return sum / target.Count;
    }

    private List<double> MSEPrime(List<double> target, List<double> output)
    {
        var gradient = new List<double>();
        for (int i = 0; i < target.Count; i++)
            gradient.Add(2 * (output[i] - target[i]));
        return gradient;
    }

    // ----------------- Training -----------------
    public void Train(int episodes, float learningRate, List<List<double>> X, List<List<double>> Y, bool show = false)
    {
        for (int e = 0; e < episodes; e++)
        {
            double error = 0;
            for (int i = 0; i < X.Count; i++)
            {
                var output = ForwardPass(X[i]);
                error += MSE(Y[i], output);

                var gradient = MSEPrime(Y[i], output);
                for (int j = layers.Count - 1; j >= 0; j--)
                    gradient = layers[j].BackwardPass(gradient, learningRate);
            }
            error /= X.Count;

            if (show && (e == 0 || e % Math.Max(1, episodes / 10) == 0))
                Console.WriteLine($"Episode {e}, Error: {error}");
        }
    }

    // ----------------- Mutate -----------------
    public void Mutate(float learningRate, float mutationChance)
    {
        foreach (var layer in layers)
        {
            if (layer.Type == "Dense" || layer.Type == "Conv2D")
                layer.Mutate(learningRate, mutationChance);
        }
    }

    // ----------------- Save -----------------
    public void Save(string path)
    {
        using (var writer = new StreamWriter(path))
        {
            foreach (var layer in layers)
            {
                writer.WriteLine(layer.Type);

                if (layer is Dense dense)
                {
                    writer.WriteLine($"{dense.Inputs} {dense.Outputs}");
                    foreach (var row in dense.GetWeights())
                        writer.WriteLine(string.Join(",", row));
                    writer.WriteLine();
                    writer.WriteLine(string.Join(",", dense.GetBias()));
                }
                else if (layer is Conv2D conv)
                {
                    writer.WriteLine($"{conv.Inputs} {conv.Outputs}");
                    writer.WriteLine($"{conv.inputHeight} {conv.inputWidth} {conv.kernelHeight} {conv.kernelWidth} {conv.numKernels} {conv.stride}");

                    foreach (var kernel in conv.GetKernels())
                    {
                        for (int i = 0; i < conv.kernelHeight; i++)
                        {
                            List<string> row = new List<string>();
                            for (int j = 0; j < conv.kernelWidth; j++)
                                row.Add(kernel[i, j].ToString());
                            writer.WriteLine(string.Join(",", row));
                        }
                        writer.WriteLine();
                    }
                    writer.WriteLine(string.Join(",", conv.GetBiases()));
                }
                else if (layer is MaxPooling2D pool)
                {
                    writer.WriteLine($"{pool.Inputs} {pool.Outputs}");
                    writer.WriteLine($"{pool.inputHeight} {pool.inputWidth} {pool.poolHeight} {pool.poolWidth} {pool.stride}");
                }
            }
        }
    }

    // ----------------- Load -----------------
    public void Load(string path)
    {
        layers.Clear();
        using (var reader = new StreamReader(path))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                if (line == "Dense")
                {
                    string[] io = reader.ReadLine().Split(' ');
                    int inputs = int.Parse(io[0]);
                    int outputs = int.Parse(io[1]);
                    var dense = new Dense(inputs, outputs);

                    var weights = new List<List<double>>();
                    for (int i = 0; i < inputs; i++)
                    {
                        string weightLine = reader.ReadLine().Trim();
                        var row = new List<double>();
                        foreach (var val in weightLine.Split(',')) row.Add(double.Parse(val));
                        weights.Add(row);
                    }
                    reader.ReadLine();
                    string biasLine = reader.ReadLine().Trim();
                    var bias = new List<double>();
                    foreach (var val in biasLine.Split(',')) bias.Add(double.Parse(val));

                    dense.SetWeights(weights);
                    dense.SetBias(bias);
                    layers.Add(dense);
                }
                else if (line == "Conv2D")
                {
                    string[] io = reader.ReadLine().Split(' ');
                    int inputs = int.Parse(io[0]);
                    int outputs = int.Parse(io[1]);

                    string[] cfg = reader.ReadLine().Split(' ');
                    int h = int.Parse(cfg[0]);
                    int w = int.Parse(cfg[1]);
                    int kh = int.Parse(cfg[2]);
                    int kw = int.Parse(cfg[3]);
                    int nk = int.Parse(cfg[4]);
                    int stride = int.Parse(cfg[5]);

                    var conv = new Conv2D(h, w, kh, kw, nk, stride);

                    var kernels = new List<double[,]>();
                    for (int k = 0; k < nk; k++)
                    {
                        double[,] kernel = new double[kh, kw];
                        for (int i = 0; i < kh; i++)
                        {
                            string rowLine = reader.ReadLine().Trim();
                            var parts = rowLine.Split(',');
                            for (int j = 0; j < kw; j++)
                                kernel[i, j] = double.Parse(parts[j]);
                        }
                        kernels.Add(kernel);
                        reader.ReadLine();
                    }

                    string biasLine = reader.ReadLine().Trim();
                    var biases = new List<double>();
                    foreach (var val in biasLine.Split(',')) biases.Add(double.Parse(val));

                    conv.SetKernels(kernels);
                    conv.SetBiases(biases);
                    layers.Add(conv);
                }
                else if (line == "MaxPooling2D")
                {
                    string[] io = reader.ReadLine().Split(' ');
                    int inputs = int.Parse(io[0]);
                    int outputs = int.Parse(io[1]);

                    string[] cfg = reader.ReadLine().Split(' ');
                    int h = int.Parse(cfg[0]);
                    int w = int.Parse(cfg[1]);
                    int ph = int.Parse(cfg[2]);
                    int pw = int.Parse(cfg[3]);
                    int stride = int.Parse(cfg[4]);

                    layers.Add(new MaxPooling2D(h, w, ph, pw, stride));
                }
                else if (line == "Relu") layers.Add(new Relu());
                else if (line == "Sigmoid") layers.Add(new Sigmoid());
                else if (line == "Tanh") layers.Add(new Tanh());
            }
        }
    }

    // ----------------- LoadFrom (deep copy) -----------------
    public void LoadFrom(Network other)
    {
        this.layers = new List<Layer>();
        foreach (var layer in other.layers)
        {
            if (layer is Dense d)
            {
                Dense copy = new Dense(d.Inputs, d.Outputs);
                copy.SetWeights(d.GetWeights());
                copy.SetBias(d.GetBias());
                this.layers.Add(copy);
            }
            else if (layer is Conv2D c)
            {
                Conv2D copy = new Conv2D(c.inputHeight, c.inputWidth, c.kernelHeight, c.kernelWidth, c.numKernels, c.stride);
                copy.SetKernels(c.GetKernels());
                copy.SetBiases(c.GetBiases());
                this.layers.Add(copy);
            }
            else if (layer is MaxPooling2D p)
                this.layers.Add(new MaxPooling2D(p.inputHeight, p.inputWidth, p.poolHeight, p.poolWidth, p.stride));
            else if (layer is Sigmoid) this.layers.Add(new Sigmoid());
            else if (layer is Tanh) this.layers.Add(new Tanh());
            else if (layer is Relu) this.layers.Add(new Relu());
        }
    }
}
