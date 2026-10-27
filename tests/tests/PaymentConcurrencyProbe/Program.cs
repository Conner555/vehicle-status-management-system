
using System.Net.Http.Headers;
using System.Net.Http.Json;

string token =
    Environment.GetEnvironmentVariable("FLEETFLOW_TEST_TOKEN")
    ?? throw new InvalidOperationException(
        "FLEETFLOW_TEST_TOKEN is missing.");

string? orderIdText =
    Environment.GetEnvironmentVariable("FLEETFLOW_TEST_ORDER_ID");

if (!int.TryParse(orderIdText, out int orderId)
    || orderId <= 0)
{
    throw new InvalidOperationException(
        "FLEETFLOW_TEST_ORDER_ID is invalid.");
}

using HttpClient client = new()
{
    BaseAddress = new Uri("http://localhost:5129")
};

client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);

// 两个请求使用相同订单和支付方式
var payload = new
{
    method = "MockWallet"
};

// 创建统一开始信号
var startSignal = new TaskCompletionSource<bool>(
    TaskCreationOptions.RunContinuationsAsynchronously);

async Task<(int Status, string Body)> PayAsync(int number)
{
    await startSignal.Task;

    using var response = await client.PostAsJsonAsync(
        $"/api/rental-orders/{orderId}/pay",
        payload);

    string body =
        await response.Content.ReadAsStringAsync();

    Console.WriteLine(
        $"Request {number}: {(int)response.StatusCode}");

    return ((int)response.StatusCode, body);
}

// 两个任务同时准备
Task<(int Status, string Body)> taskA = PayAsync(1);
Task<(int Status, string Body)> taskB = PayAsync(2);

// 统一释放
startSignal.SetResult(true);

// 等待两个请求全部完成
var results = await Task.WhenAll(taskA, taskB);

Console.WriteLine("\n--- Payment Results ---");

foreach (var result in results)
{
    Console.WriteLine(
        $"{result.Status}: {result.Body}");
}

int successCount = results.Count(x => x.Status == 200);
int conflictCount = results.Count(x => x.Status == 409);

Console.WriteLine(
    $"Success: {successCount}, Conflict: {conflictCount}");

if (successCount == 1 && conflictCount == 1)
{
    Console.WriteLine("PASS: Concurrent payment behavior.");
}
else
{
    Console.WriteLine("CHECK: Unexpected payment results.");
}
