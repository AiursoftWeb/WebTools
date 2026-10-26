using System.Net;

namespace Aiursoft.WebTools.Tests.Services;

[TestClass]
public class EmbeddingHelperTests
{
    [TestMethod]
    [DataRow(HttpStatusCode.BadRequest, "input context length exceeded")]
    [DataRow(HttpStatusCode.RequestEntityTooLarge, "the input length exceeds the context length")]
    [DataRow(HttpStatusCode.BadRequest, "maximum context window reached")]
    [DataRow(HttpStatusCode.RequestEntityTooLarge, "CONTEXT LIMIT EXCEEDED")]
    public void ExplicitContextErrorsAllowInputReduction(HttpStatusCode status, string error)
    {
        Assert.IsTrue(EmbeddingHelper.IsContextLengthError(status, error));
    }

    [TestMethod]
    [DataRow(HttpStatusCode.BadRequest, "Request body length exceeds the limit")]
    [DataRow(HttpStatusCode.RequestEntityTooLarge, "Request body length exceeds the limit")]
    [DataRow(HttpStatusCode.BadRequest, "Unknown model")]
    [DataRow(HttpStatusCode.RequestEntityTooLarge, "Context service unavailable")]
    [DataRow(HttpStatusCode.InternalServerError, "input context length exceeded")]
    [DataRow(HttpStatusCode.TooManyRequests, "input context length exceeded")]
    [DataRow(HttpStatusCode.OK, "input context length exceeded")]
    [DataRow(HttpStatusCode.RequestEntityTooLarge, "maximum length exceeded")]
    public void UnrelatedErrorsDoNotAllowInputReduction(HttpStatusCode status, string error)
    {
        Assert.IsFalse(EmbeddingHelper.IsContextLengthError(status, error));
    }

    [TestMethod]
    public void NormalizedVectorsPreserveCosineRanking()
    {
        float[] query = [3, 4];
        float[] matching = [6, 8];
        float[] opposite = [-3, -4];
        float[] orthogonal = [-4, 3];
        foreach (var vector in new[] { query, matching, opposite, orthogonal })
            EmbeddingHelper.Normalize(vector);

        Assert.AreEqual(0.6f, query[0], 0.000001f);
        Assert.AreEqual(0.8f, query[1], 0.000001f);
        Assert.AreEqual(1f, EmbeddingHelper.CosineSimilarity(query, matching), 0.000001f);
        Assert.AreEqual(-1f, EmbeddingHelper.CosineSimilarity(query, opposite), 0.000001f);
        Assert.AreEqual(0f, EmbeddingHelper.CosineSimilarity(query, orthogonal), 0.000001f);
    }

    [TestMethod]
    public void ZeroVectorNormalizationPreservesZeros()
    {
        float[] vector = [0, 0];
        EmbeddingHelper.Normalize(vector);
        CollectionAssert.AreEqual(new[] { 0f, 0f }, vector);
    }

    [TestMethod]
    public void StoredVectorBytesRemainCompatibleWithExistingApplications()
    {
        // Existing application columns store native float32 bytes using Buffer.BlockCopy.
        float[] vector = [0.25f, -1.5f, 0f, 42.125f];
        var existingBytes = vector.SelectMany(BitConverter.GetBytes).ToArray();

        CollectionAssert.AreEqual(existingBytes, EmbeddingHelper.Serialize(vector));
        CollectionAssert.AreEqual(vector, EmbeddingHelper.Deserialize(existingBytes)!);
    }

    [TestMethod]
    public void MissingOrIncompleteStoredVectorsRemainUnreadable()
    {
        Assert.IsNull(EmbeddingHelper.Deserialize(null));
        Assert.IsNull(EmbeddingHelper.Deserialize(new byte[3]));
    }

    [TestMethod]
    public void EmptyVectorsPreserveTheExistingStorageContract()
    {
        CollectionAssert.AreEqual(Array.Empty<byte>(), EmbeddingHelper.Serialize([]));
        CollectionAssert.AreEqual(Array.Empty<float>(), EmbeddingHelper.Deserialize([])!);
    }
}
