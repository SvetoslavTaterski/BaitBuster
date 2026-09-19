using Microsoft.ML;

namespace BaitBuster.MlTraining;

/// <summary>Един изпробван алгоритъм: име и стъпката, която го обучава.</summary>
internal sealed record Candidate(string Name, IEstimator<ITransformer> Trainer);

internal static class Candidates
{
    /// <summary>
    /// Алгоритмите, между които избираме. Всичките решават една и съща задача
    /// (двоична класификация върху разредени текстови признаци), но по различен
    /// начин — затова сравнението има смисъл:
    ///
    ///   SdcaLogisticRegression   линеен, стохастичен двойствен координатен възход
    ///   LbfgsLogisticRegression  линеен, квази-нютонова оптимизация
    ///   AveragedPerceptron       онлайн алгоритъм, устойчив на много признаци
    ///   LinearSvm                максимизира отстоянието между класовете
    ///   FastTree                 ансамбъл от дървета чрез бустинг
    ///   FastForest               ансамбъл от дървета чрез багинг (Random Forest)
    ///
    /// FastTree и FastForest са различни въпреки общата основа: при бустинга
    /// дърветата се строят последователно и всяко коригира грешките на
    /// предходните, а при багинга — независимо върху различни подизвадки, след
    /// което гласуват. Затова FastTree не може да мине за Random Forest.
    ///
    /// Perceptron, SVM и FastForest връщат само суров резултат (Score), не
    /// вероятност. MlClassifierRule обаче праща вероятност в доклада, затова
    /// към тях се добавя калибрация по Плат — иначе изборът на такъв модел би
    /// счупил правилото.
    /// </summary>
    public static Candidate[] Build(MLContext ml, string labelColumn, string featureColumn)
    {
        var calibrator = ml.BinaryClassification.Calibrators
            .Platt(labelColumnName: labelColumn, scoreColumnName: "Score");

        return
        [
            new("SdcaLogisticRegression",
                ml.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumn, featureColumn)),

            new("LbfgsLogisticRegression",
                ml.BinaryClassification.Trainers.LbfgsLogisticRegression(labelColumn, featureColumn)),

            new("AveragedPerceptron",
                ml.BinaryClassification.Trainers.AveragedPerceptron(labelColumn, featureColumn)
                  .Append(calibrator)),

            new("LinearSvm",
                ml.BinaryClassification.Trainers.LinearSvm(labelColumn, featureColumn)
                  .Append(calibrator)),

            new("FastTree",
                ml.BinaryClassification.Trainers.FastTree(
                    labelColumn, featureColumn, numberOfLeaves: 20, numberOfTrees: 100)),

            new("FastForest",
                ml.BinaryClassification.Trainers.FastForest(
                    labelColumnName: labelColumn, featureColumnName: featureColumn,
                    numberOfLeaves: 20, numberOfTrees: 100)
                  .Append(calibrator)),
        ];
    }
}
