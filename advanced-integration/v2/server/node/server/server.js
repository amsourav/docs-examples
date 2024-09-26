import express from "express";
import "dotenv/config";
import {
  ApiError,
  Client,
  Environment,
  LogLevel,
  OrdersController,
  PaymentsController,
  ShippingType,
} from "@paypal/paypal-server-sdk";
import bodyParser from "body-parser";

const app = express();
app.use(bodyParser.json());

const { PAYPAL_CLIENT_ID, PAYPAL_CLIENT_SECRET, PORT = 8080 } = process.env;

const client = new Client({
  clientCredentialsAuthCredentials: {
    oAuthClientId: PAYPAL_CLIENT_ID,
    oAuthClientSecret: PAYPAL_CLIENT_SECRET,
  },
  timeout: 0,
  environment: Environment.Sandbox,
  logging: {
    logLevel: LogLevel.Info,
    logRequest: {
      logBody: true,
    },
    logResponse: {
      logHeaders: true,
    },
  },
});

const ordersController = new OrdersController(client);
const paymentsController = new PaymentsController(client);

/**
 * Create an order to start the transaction.
 * @see https://developer.paypal.com/docs/api/orders/v2/#orders_create
 */
const createOrder = async (cart) => {
  const payload = {
    body: {
      intent: "CAPTURE",
      purchaseUnits: [
        {
          amount: {
            currencyCode: "USD",
            value: "100",
          },
          shipping: {
            options: [
              {
                id: "1",
                label: "Free Shipping",
                selected: true,
                type: ShippingType.SHIPPING,
                amount: {
                  currencyCode: "USD",
                  value: "0",
                },
              },
              {
                id: "2",
                label: "Expedited Shipping",
                selected: false,
                type: ShippingType.SHIPPING,
                amount: {
                  currencyCode: "USD",
                  value: "5",
                },
              },
            ],
          },
        },
      ],
    },
  };

  try {
    const { body, ...httpResponse } = await ordersController.ordersCreate(
      payload
    );
    // Get more response info...
    // const { statusCode, headers } = httpResponse;
    return {
      jsonResponse: JSON.parse(body),
      httpStatusCode: httpResponse.statusCode,
    };
  } catch (error) {
    if (error instanceof ApiError) {
      // const { statusCode, headers } = error;
      throw new Error(error.message);
    }
  }
};

app.post("/api/orders", async (req, res) => {
  try {
    // use the cart information passed from the front-end to calculate the order amount detals
    const { cart } = req.body;
    const { jsonResponse, httpStatusCode } = await createOrder(cart);
    res.status(httpStatusCode).json(jsonResponse);
  } catch (error) {
    console.error("Failed to create order:", error);
    res.status(500).json({ error: "Failed to create order." });
  }
});

const refundCapturedPayment = async (capturedPaymentId) => {
  const collect = {
    captureId: capturedPaymentId,
  };

  try {
    const { body, ...httpResponse } = await paymentsController.capturesRefund(
      collect
    );
    // Get more response info...
    // const { statusCode, headers } = httpResponse;
    return {
      jsonResponse: JSON.parse(body),
      httpStatusCode: httpResponse.statusCode,
    };
  } catch (error) {
    if (error instanceof ApiError) {
      // const { statusCode, headers } = error;
      throw new Error(error.message);
    }
  }
};

// refundCapturedPayment route
app.post("/api/payments/refund", async (req, res) => {
  try {
    const { capturedPaymentId } = req.body;
    const { jsonResponse, httpStatusCode } = await refundCapturedPayment(
      capturedPaymentId
    );
    res.status(httpStatusCode).json(jsonResponse);
  } catch (error) {
    console.error("Failed refund captured payment:", error);
    res.status(500).json({ error: "Failed refund captured payment." });
  }
});

/**
 * Capture payment for the created order to complete the transaction.
 * @see https://developer.paypal.com/docs/api/orders/v2/#orders_capture
 */
const captureOrder = async (orderID) => {
  const collect = {
    id: orderID,
  };

  try {
    const { body, ...httpResponse } = await ordersController.ordersCapture(
      collect
    );
    // Get more response info...
    // const { statusCode, headers } = httpResponse;
    return {
      jsonResponse: JSON.parse(body),
      httpStatusCode: httpResponse.statusCode,
    };
  } catch (error) {
    if (error instanceof ApiError) {
      // const { statusCode, headers } = error;
      throw new Error(error.message);
    }
  }
};

app.post("/api/orders/:orderID/capture", async (req, res) => {
  try {
    const { orderID } = req.params;
    const { jsonResponse, httpStatusCode } = await captureOrder(orderID);
    res.status(httpStatusCode).json(jsonResponse);
  } catch (error) {
    console.error("Failed to create order:", error);
    res.status(500).json({ error: "Failed to capture order." });
  }
});

/**
 * Authorize payment for the created order to complete the transaction.
 * @see https://developer.paypal.com/docs/api/orders/v2/#orders_authorize
 */
const authorizeOrder = async (orderID) => {
  const collect = {
    id: orderID,
  };

  try {
    const { body, ...httpResponse } = await ordersController.ordersAuthorize(
      collect
    );
    // Get more response info...
    // const { statusCode, headers } = httpResponse;
    return {
      jsonResponse: JSON.parse(body),
      httpStatusCode: httpResponse.statusCode,
    };
  } catch (error) {
    if (error instanceof ApiError) {
      // const { statusCode, headers } = error;
      throw new Error(error.message);
    }
  }
};

app.post("/api/orders/:orderID/authorize", async (req, res) => {
  try {
    const { orderID } = req.params;
    const { jsonResponse, httpStatusCode } = await authorizeOrder(orderID);
    res.status(httpStatusCode).json(jsonResponse);
  } catch (error) {
    console.error("Failed to create order:", error);
    res.status(500).json({ error: "Failed to capture order." });
  }
});

/**
 * Capture payment for the created order to complete the transaction.
 * @see https://developer.paypal.com/docs/api/orders/v2/#orders_capture
 */
const authorizationsCapture = async (authorizationId) => {
  const collect = {
    authorizationId: authorizationId,
  };

  try {
    const { body, ...httpResponse } =
      await paymentsController.authorizationsCapture(collect);
    // Get more response info...
    // const { statusCode, headers } = httpResponse;
    return {
      jsonResponse: JSON.parse(body),
      httpStatusCode: httpResponse.statusCode,
    };
  } catch (error) {
    if (error instanceof ApiError) {
      // const { statusCode, headers } = error;
      throw new Error(error.message);
    }
  }
};

app.post("/api/orders/:authorizationId/captureAuthorize", async (req, res) => {
  try {
    const { authorizationId } = req.params;
    const { jsonResponse, httpStatusCode } = await authorizationsCapture(
      authorizationId
    );
    res.status(httpStatusCode).json(jsonResponse);
  } catch (error) {
    console.error("Failed to create order:", error);
    res.status(500).json({ error: "Failed to capture order." });
  }
});

app.listen(PORT, () => {
  console.log(`Node server listening at http://localhost:${PORT}/`);
});
