
#!/usr/bin/env bash
set -euo pipefail

BASE_URL="http://localhost:5129"
REQUEST_COUNT=50

# JWT 不写死在脚本中
read -rsp "请输入 Admin JWT Token: " TOKEN
echo

if [[ -z "$TOKEN" ]]; then
    echo "Token 不能为空"
    exit 1
fi

# 检查 Redis
if ! redis-cli ping | grep -q PONG; then
    echo "Redis 未正常运行"
    exit 1
fi

# 删除 Dashboard 缓存
redis-cli DEL FleetFlow:dashboard:summary:v1

# 保存每个请求的 HTTP 状态码
RESULT_DIR=$(mktemp -d)
trap 'rm -rf "$RESULT_DIR"' EXIT

echo "开始发送 $REQUEST_COUNT 个并发请求..."

for i in $(seq 1 "$REQUEST_COUNT"); do
    (
        curl --silent \
            --output /dev/null \
            --max-time 15 \
            --write-out '%{http_code}\n' \
            -H "Authorization: Bearer $TOKEN" \
            "$BASE_URL/api/dashboard/summary" \
            > "$RESULT_DIR/$i" \
            || echo "CURL_ERROR" > "$RESULT_DIR/$i"
    ) &
done

wait

echo "HTTP 状态码统计："
cat "$RESULT_DIR"/* | sort | uniq -c

echo "缓存剩余 TTL："
redis-cli TTL FleetFlow:dashboard:summary:v1

echo "并发测试完成"
