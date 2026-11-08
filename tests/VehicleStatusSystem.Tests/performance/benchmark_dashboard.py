
import os
import time
import statistics
import urllib.request
import urllib.error
from concurrent.futures import ThreadPoolExecutor, as_completed

URL = "http://localhost:5129/api/dashboard/summary"

TOKEN = os.environ.get("FLEETFLOW_ADMIN_TOKEN", "").strip()

TOTAL_REQUESTS = 5000
CONCURRENCY = 100

if not TOKEN:
    raise SystemExit(
        "请先设置 FLEETFLOW_ADMIN_TOKEN 环境变量"
    )


def send_request():
    request = urllib.request.Request(
        URL,
        headers={
            "Authorization": f"Bearer {TOKEN}"
        }
    )

    start = time.perf_counter()

    try:
        with urllib.request.urlopen(
            request,
            timeout=15
        ) as response:
            response.read()
            status = response.status

    except urllib.error.HTTPError as ex:
        status = ex.code

    except Exception:
        status = 0

    elapsed_ms = (
        time.perf_counter() - start
    ) * 1000

    return status, elapsed_ms


def percentile(values, p):
    values = sorted(values)

    position = (len(values) - 1) * p / 100
    lower = int(position)
    upper = min(lower + 1, len(values) - 1)

    fraction = position - lower

    return (
        values[lower] * (1 - fraction)
        + values[upper] * fraction
    )


# 测试前单独预热，避免混入测试数据
for _ in range(5):
    status, _ = send_request()
    if status != 200:
        raise SystemExit(
            f"预热失败，HTTP {status}"
        )

print(
    f"测试开始：requests={TOTAL_REQUESTS}, "
    f"concurrency={CONCURRENCY}"
)

start_time = time.perf_counter()
results = []

with ThreadPoolExecutor(
    max_workers=CONCURRENCY
) as executor:

    futures = [
        executor.submit(send_request)
        for _ in range(TOTAL_REQUESTS)
    ]

    for future in as_completed(futures):
        results.append(future.result())

duration = time.perf_counter() - start_time

success_times = [
    elapsed
    for status, elapsed in results
    if status == 200
]

success_count = len(success_times)
error_count = len(results) - success_count

print(f"总请求数: {len(results)}")
print(f"成功请求: {success_count}")
print(f"失败请求: {error_count}")
print(f"测试耗时: {duration:.3f} s")
print(f"成功吞吐量: {success_count / duration:.2f} RPS")

if success_times:
    print(
        f"平均延迟: "
        f"{statistics.mean(success_times):.2f} ms"
    )
    print(
        f"P95: {percentile(success_times, 95):.2f} ms"
    )
    print(
        f"P99: {percentile(success_times, 99):.2f} ms"
    )
    print(
        f"最大延迟: {max(success_times):.2f} ms"
    )
else:
    print("没有成功请求，无法统计延迟")
