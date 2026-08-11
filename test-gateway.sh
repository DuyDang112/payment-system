#!/bin/bash

echo "🚀 Simple Payment API Test"
echo "=========================="
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# API Gateway URL
GATEWAY_URL="http://localhost:5000"

echo "📍 Testing API Gateway at: $GATEWAY_URL"
echo ""

# Test 1: Create a simple payment
echo -e "${YELLOW}Test: Create Payment${NC}"
echo "POST $GATEWAY_URL/api/payments"
echo ""

PAYMENT_RESPONSE=$(curl -k -s -X POST "$GATEWAY_URL/api/payments" \
  -H "Content-Type: application/json" \
  -d '{
    "merchantId": "merchant_low_risk",
    "customerId": "customer-456",
    "amount": 100.00,
    "currency": "USD",
    "idempotencyKey": "idempotencyKey-test-key-010",
    "paymentMethodToken": "pm_token_visa",
    "metadata": {
      "customerEmail": "customer@example.com",
      "orderId": "order-789"
    }
  }')

echo "Response:"
echo "$PAYMENT_RESPONSE" | jq '.' 2>/dev/null || echo "$PAYMENT_RESPONSE"
echo ""

# Extract payment ID for subsequent tests
PAYMENT_ID=$(echo "$PAYMENT_RESPONSE" | jq -r '.id // empty' 2>/dev/null)

if [ -n "$PAYMENT_ID" ] && [ "$PAYMENT_ID" != "null" ]; then
    echo -e "${GREEN}✅ Payment created successfully with ID: $PAYMENT_ID${NC}"
    echo ""

    # Test 2: Get payment by ID
    echo -e "${YELLOW}Test: Get Payment by ID${NC}"
    echo "GET $GATEWAY_URL/api/payments/$PAYMENT_ID"
    echo ""

    GET_RESPONSE=$(curl -k -s "$GATEWAY_URL/api/payments/$PAYMENT_ID")
    echo "Response:"
    echo "$GET_RESPONSE" | jq '.' 2>/dev/null || echo "$GET_RESPONSE"
    echo ""

    echo -e "${GREEN}✅ Tests completed successfully!${NC}"
    echo ""
    echo "📊 Observability:"
    echo "• Prometheus: http://localhost:9090"
    echo "• Tempo: http://localhost:3200"
    echo "• Grafana: http://localhost:3000"
    echo "• Search for traces with service name: 'PaymentProcessing'"

else
    echo -e "${RED}❌ Payment creation failed${NC}"
    echo ""
    echo "Check if API Gateway is running on $GATEWAY_URL"
    echo "Try: curl -s $GATEWAY_URL/health"
fi

echo ""
