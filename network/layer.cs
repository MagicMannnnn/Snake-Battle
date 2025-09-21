using System;
using System.Collections.Generic;

public abstract class Layer
{
    public int Inputs { get; protected set; }
    public int Outputs { get; protected set; }
    public string Type { get; protected set; }
    protected List<double> InputCache;

    public virtual List<double> ForwardPass(List<double> input)
    {
        InputCache = new List<double>(input);
        return new List<double>(input);
    }

    public virtual List<double> BackwardPass(List<double> outputGradient, float learningRate)
    {
        return new List<double>(outputGradient);
    }

    public virtual void Mutate(float learningRate, float mutationChance) { }

    public virtual List<List<double>> GetWeights() { return null; }
    public virtual List<double> GetBias() { return null; }
    public virtual void SetWeights(List<List<double>> weights) { }
    public virtual void SetBias(List<double> bias) { }
}

// ---------------- Dense ----------------
public class Dense : Layer
{
    private List<List<double>> weights;
    private List<double> bias;
    private static Random rand = new Random();

    public Dense(int inputs, int outputs)
    {
        Inputs = inputs;
        Outputs = outputs;
        Type = "Dense";

        weights = new List<List<double>>();
        bias = new List<double>();

        // Xavier/Glorot initialization
        double limit = Math.Sqrt(6.0 / (inputs + outputs));
        for (int i = 0; i < inputs; i++)
        {
            var row = new List<double>();
            for (int j = 0; j < outputs; j++)
                row.Add(rand.NextDouble() * 2 * limit - limit);
            weights.Add(row);
        }

        for (int i = 0; i < outputs; i++)
            bias.Add(0.0); // start small
    }

    public override List<double> ForwardPass(List<double> input)
    {
        InputCache = new List<double>(input);
        var output = new List<double>();

        for (int i = 0; i < Outputs; i++)
        {
            double sum = 0;
            for (int j = 0; j < Inputs; j++)
                sum += weights[j][i] * input[j];
            sum += bias[i];

            // NaN/Inf safety
            if (double.IsNaN(sum) || double.IsInfinity(sum))
                sum = 0.0;

            output.Add(sum);
        }
        return output;
    }

    public override List<double> BackwardPass(List<double> outputGradient, float learningRate)
    {
        var inputGradient = new List<double>(new double[Inputs]);
        var weightGradient = new double[Inputs, Outputs];

        // Compute gradients
        for (int i = 0; i < Inputs; i++)
        {
            for (int j = 0; j < Outputs; j++)
            {
                weightGradient[i, j] = outputGradient[j] * InputCache[i];
                inputGradient[i] += weights[i][j] * outputGradient[j];
            }
        }

        // Gradient clipping
        double clipValue = 5.0;
        for (int i = 0; i < Inputs; i++)
        {
            for (int j = 0; j < Outputs; j++)
            {
                double grad = Math.Max(-clipValue, Math.Min(clipValue, weightGradient[i, j]));
                weights[i][j] -= learningRate * grad;
            }
        }

        for (int i = 0; i < Outputs; i++)
        {
            double grad = Math.Max(-clipValue, Math.Min(clipValue, outputGradient[i]));
            bias[i] -= learningRate * grad;
        }

        return inputGradient;
    }



    public override void Mutate(float learningRate, float mutationChance)
    {
        var rand = new Random();
        for (int i = 0; i < Inputs; i++)
        {
            for (int j = 0; j < Outputs; j++)
            {
                if (rand.NextDouble() < mutationChance)
                    weights[i][j] += (rand.NextDouble() * 2 - 1) * learningRate;
            }
        }
    }

    public override List<List<double>> GetWeights() => weights;
    public override List<double> GetBias() => bias;
    public override void SetWeights(List<List<double>> w) => weights = w;
    public override void SetBias(List<double> b) => bias = b;
}

// ---------------- Activations ----------------
public class Sigmoid : Layer
{
    public Sigmoid() { Type = "Sigmoid"; }

    private double Func(double x) => 1 / (1 + Math.Exp(-x));
    private double Deriv(double x) => Func(x) * (1 - Func(x));

    public override List<double> ForwardPass(List<double> input)
    {
        InputCache = new List<double>(input);
        var output = new List<double>();
        foreach (var val in input) output.Add(Func(val));
        return output;
    }

    public override List<double> BackwardPass(List<double> outputGradient, float learningRate)
    {
        var inputGradient = new List<double>();
        for (int i = 0; i < outputGradient.Count; i++)
            inputGradient.Add(outputGradient[i] * Deriv(InputCache[i]));
        return inputGradient;
    }
}

public class Tanh : Layer
{
    public Tanh() { Type = "Tanh"; }

    private double Func(double x) => Math.Tanh(x);
    private double Deriv(double x) => 1 - Math.Pow(Math.Tanh(x), 2);

    public override List<double> ForwardPass(List<double> input)
    {
        InputCache = new List<double>(input);
        var output = new List<double>();
        foreach (var val in input) output.Add(Func(val));
        return output;
    }

    public override List<double> BackwardPass(List<double> outputGradient, float learningRate)
    {
        var inputGradient = new List<double>();
        for (int i = 0; i < outputGradient.Count; i++)
            inputGradient.Add(outputGradient[i] * Deriv(InputCache[i]));
        return inputGradient;
    }
}

public class Relu : Layer
{
    public Relu() { Type = "Relu"; }

    private double Func(double x) => Math.Max(0, x);
    private double Deriv(double x) => x > 0 ? 1 : 0;

    public override List<double> ForwardPass(List<double> input)
    {
        InputCache = new List<double>(input);
        var output = new List<double>();
        foreach (var val in input) output.Add(Func(val));
        return output;
    }

    public override List<double> BackwardPass(List<double> outputGradient, float learningRate)
    {
        var inputGradient = new List<double>();
        for (int i = 0; i < outputGradient.Count; i++)
            inputGradient.Add(outputGradient[i] * Deriv(InputCache[i]));
        return inputGradient;
    }
}

// ---------------- Conv2D ----------------
public class Conv2D : Layer
{
    public int inputHeight, inputWidth, kernelHeight, kernelWidth, stride, numKernels;
    private List<double[,]> kernels;
    private List<double> biases;
    private double[,] input2D;
    private static Random rand = new Random();

    public Conv2D(int inputHeight, int inputWidth, int kernelHeight, int kernelWidth, int numKernels, int stride = 1)
    {
        this.inputHeight = inputHeight;
        this.inputWidth = inputWidth;
        this.kernelHeight = kernelHeight;
        this.kernelWidth = kernelWidth;
        this.numKernels = numKernels;
        this.stride = stride;

        Type = "Conv2D";
        kernels = new List<double[,]>();
        biases = new List<double>();

        // Xavier/Glorot initialization
        double limit = Math.Sqrt(6.0 / (kernelHeight * kernelWidth + numKernels));
        for (int k = 0; k < numKernels; k++)
        {
            double[,] kernel = new double[kernelHeight, kernelWidth];
            for (int i = 0; i < kernelHeight; i++)
                for (int j = 0; j < kernelWidth; j++)
                    kernel[i, j] = rand.NextDouble() * 2 * limit - limit;
            kernels.Add(kernel);
            biases.Add(0.0);
        }

        int outH = (inputHeight - kernelHeight) / stride + 1;
        int outW = (inputWidth - kernelWidth) / stride + 1;
        Outputs = outH * outW * numKernels;
        Inputs = inputHeight * inputWidth;
    }

    public override List<double> ForwardPass(List<double> input)
    {
        input2D = new double[inputHeight, inputWidth];
        for (int i = 0; i < inputHeight; i++)
            for (int j = 0; j < inputWidth; j++)
                input2D[i, j] = input[i * inputWidth + j];

        List<double> output = new List<double>();
        foreach (var (kernel, bias) in Zip())
        {
            for (int i = 0; i <= inputHeight - kernelHeight; i += stride)
            {
                for (int j = 0; j <= inputWidth - kernelWidth; j += stride)
                {
                    double sum = 0;
                    for (int ki = 0; ki < kernelHeight; ki++)
                        for (int kj = 0; kj < kernelWidth; kj++)
                            sum += kernel[ki, kj] * input2D[i + ki, j + kj];
                    sum += bias;

                    if (double.IsNaN(sum) || double.IsInfinity(sum))
                        sum = 0.0;

                    output.Add(sum);
                }
            }
        }
        return output;
    }

    public override List<double> BackwardPass(List<double> outputGradient, float learningRate)
    {
        double[,] inp = input2D;
        int gradIndex = 0;
        double clipValue = 5.0;

        for (int k = 0; k < numKernels; k++)
        {
            double[,] kernel = kernels[k];
            for (int i = 0; i <= inputHeight - kernelHeight; i += stride)
            {
                for (int j = 0; j <= inputWidth - kernelWidth; j += stride)
                {
                    double grad = outputGradient[gradIndex++];
                    grad = Math.Max(-clipValue, Math.Min(clipValue, grad));

                    for (int ki = 0; ki < kernelHeight; ki++)
                        for (int kj = 0; kj < kernelWidth; kj++)
                            kernel[ki, kj] -= learningRate * grad * inp[i + ki, j + kj];

                    biases[k] -= learningRate * grad;
                }
            }
        }
        return new List<double>(new double[inputHeight * inputWidth]);
    }

    private IEnumerable<(double[,], double)> Zip()
    {
        for (int i = 0; i < numKernels; i++)
            yield return (kernels[i], biases[i]);
    }

    public override void Mutate(float learningRate, float mutationChance)
    {
        var rand = new Random();
        for (int k = 0; k < numKernels; k++)
        {
            for (int i = 0; i < kernelHeight; i++)
                for (int j = 0; j < kernelWidth; j++)
                    if (rand.NextDouble() < mutationChance)
                        kernels[k][i, j] += (rand.NextDouble() * 2 - 1) * learningRate;
            if (rand.NextDouble() < mutationChance)
                biases[k] += (rand.NextDouble() * 2 - 1) * learningRate;
        }
    }

    public List<double[,]> GetKernels() => kernels;
    public List<double> GetBiases() => biases;
    public void SetKernels(List<double[,]> k) => kernels = k;
    public void SetBiases(List<double> b) => biases = b;

}

// ---------------- MaxPooling2D ----------------
public class MaxPooling2D : Layer
{
    public int inputHeight, inputWidth, poolHeight, poolWidth, stride;
    private int outHeight, outWidth;
    private int[,] maxIndices;

    public MaxPooling2D(int inputHeight, int inputWidth, int poolHeight, int poolWidth, int stride = -1)
    {
        this.inputHeight = inputHeight;
        this.inputWidth = inputWidth;
        this.poolHeight = poolHeight;
        this.poolWidth = poolWidth;
        this.stride = stride == -1 ? poolHeight : stride;

        outHeight = (inputHeight - poolHeight) / this.stride + 1;
        outWidth = (inputWidth - poolWidth) / this.stride + 1;

        Inputs = inputHeight * inputWidth;
        Outputs = outHeight * outWidth;
        Type = "MaxPooling2D";
    }

    public override List<double> ForwardPass(List<double> input)
    {
        double[,] inp = new double[inputHeight, inputWidth];
        for (int i = 0; i < inputHeight; i++)
            for (int j = 0; j < inputWidth; j++)
                inp[i, j] = input[i * inputWidth + j];

        maxIndices = new int[outHeight, outWidth];
        List<double> output = new List<double>();

        for (int i = 0; i <= inputHeight - poolHeight; i += stride)
        {
            int oi = i / stride;
            for (int j = 0; j <= inputWidth - poolWidth; j += stride)
            {
                int oj = j / stride;
                double maxVal = double.MinValue;
                int maxIdx = -1;

                for (int pi = 0; pi < poolHeight; pi++)
                    for (int pj = 0; pj < poolWidth; pj++)
                    {
                        int idx = (i + pi) * inputWidth + (j + pj);
                        double val = inp[i + pi, j + pj];
                        if (val > maxVal)
                        {
                            maxVal = val;
                            maxIdx = idx;
                        }
                    }
                output.Add(maxVal);
                maxIndices[oi, oj] = maxIdx;
            }
        }
        return output;
    }

    public override List<double> BackwardPass(List<double> outputGradient, float learningRate)
    {
        List<double> inputGradient = new List<double>(new double[inputHeight * inputWidth]);
        int gradIndex = 0;
        for (int i = 0; i < outHeight; i++)
            for (int j = 0; j < outWidth; j++)
                inputGradient[maxIndices[i, j]] += outputGradient[gradIndex++];
        return inputGradient;
    }
}
