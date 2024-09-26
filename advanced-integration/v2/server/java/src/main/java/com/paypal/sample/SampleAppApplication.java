package com.paypal.sample;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.paypal.sdk.Environment;
import com.paypal.sdk.PaypalServerSDKClient;
import com.paypal.sdk.authentication.ClientCredentialsAuthModel;
import com.paypal.sdk.controllers.OrdersController;
import com.paypal.sdk.controllers.PaymentsController;
import com.paypal.sdk.exceptions.ApiException;
import com.paypal.sdk.http.response.ApiResponse;
import com.paypal.sdk.models.*;
import org.slf4j.event.Level;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.context.annotation.Bean;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;

import java.io.IOException;
import java.util.Arrays;
import java.util.Map;

@SpringBootApplication
public class SampleAppApplication {

	@Value("${PAYPAL_CLIENT_ID}")
	private String PAYPAL_CLIENT_ID;

	@Value("${PAYPAL_CLIENT_SECRET}")
	private String PAYPAL_CLIENT_SECRET;

	public static void main(String[] args) {
		SpringApplication.run(SampleAppApplication.class, args);
	}

	@Bean
    public PaypalServerSDKClient paypalClient() {
        return new PaypalServerSDKClient.Builder()
            .loggingConfig(builder -> builder
                .level(Level.DEBUG)
                .requestConfig(logConfigBuilder -> logConfigBuilder.body(true))
                .responseConfig(logConfigBuilder -> logConfigBuilder.headers(true)))
            .httpClientConfig(configBuilder -> configBuilder
                .timeout(0))
            .environment(Environment.SANDBOX)
            .clientCredentialsAuth(new ClientCredentialsAuthModel.Builder(
                    PAYPAL_CLIENT_ID,
                    PAYPAL_CLIENT_SECRET)
                .build())
            .build();
    }

	@Controller
	@RequestMapping("/")
	public class CheckoutController {

		private final ObjectMapper objectMapper;
		private final PaypalServerSDKClient client;

		public CheckoutController(ObjectMapper objectMapper, PaypalServerSDKClient client) {
			this.objectMapper = objectMapper;
			this.client = client;
		}

		@PostMapping("/api/orders")
		public ResponseEntity<Order> createOrder(@RequestBody Map<String, Object> request) {
			try {
				String cart = objectMapper.writeValueAsString(request.get("cart"));
				Order response = createOrder(cart);
				return new ResponseEntity<>(response, HttpStatus.OK);
			} catch (Exception e) {
				e.printStackTrace();
				return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
			}
		}

		private Order createOrder(String cart) throws IOException, ApiException {
			OrdersCreateInput ordersCreateInput = new OrdersCreateInput.Builder(
					null,
					new OrderRequest.Builder(
							CheckoutPaymentIntent.fromString("AUTHORIZE"),
							Arrays.asList(
									new PurchaseUnitRequest.Builder(
											new AmountWithBreakdown.Builder(
													"USD",
													"100").build())
											.build()))
							.paymentSource(new PaymentSource.Builder()
									.card(new CardRequest.Builder()
											.attributes(new CardAttributes.Builder()
													.verification(new CardVerification.Builder()
															.method(CardVerificationMethod.SCA_WHEN_REQUIRED)
															.build())
													.build())
											.build())
									.build())
							.build()

			).build();
			OrdersController ordersController = client.getOrdersController();
			ApiResponse<Order> apiResponse = ordersController.ordersCreate(ordersCreateInput);
			return apiResponse.getResult();
		}

		@PostMapping("/api/orders/{orderID}/capture")
		public ResponseEntity<Order> captureOrder(@PathVariable String orderID) {
			try {
				Order response = captureOrders(orderID);
				return new ResponseEntity<Order>(response, HttpStatus.OK);
			} catch (Exception e) {
				e.printStackTrace();
				return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
			}
		}

		private Order captureOrders(String orderID) throws IOException, ApiException {
			OrdersCaptureInput ordersCaptureInput = new OrdersCaptureInput.Builder(
					orderID,
					null)
					.build();
			OrdersController ordersController = client.getOrdersController();
			ApiResponse<Order> apiResponse = ordersController.ordersCapture(ordersCaptureInput);
			return apiResponse.getResult();
		}

		@PostMapping("/api/orders/{orderID}/authorize")
		public ResponseEntity<OrderAuthorizeResponse> authorizeOrder(@PathVariable String orderID)
				throws IOException, ApiException {
			try {
				OrderAuthorizeResponse response = authorizeOrders(orderID);
				return new ResponseEntity<OrderAuthorizeResponse>(response, HttpStatus.OK);
			} catch (Exception e) {
				e.printStackTrace();
				return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
			}
		}

		private OrderAuthorizeResponse authorizeOrders(String orderID) throws IOException, ApiException {
			OrdersAuthorizeInput ordersAuthorizeInput = new OrdersAuthorizeInput.Builder(
					orderID, null).build();
			OrdersController ordersController = client.getOrdersController();
			ApiResponse<OrderAuthorizeResponse> apiResponse = ordersController.ordersAuthorize(ordersAuthorizeInput);
			return apiResponse.getResult();
		}

		@PostMapping("/api/payments/refund")
		public ResponseEntity<Refund> refundCapturedPayment(@RequestBody Map<String, String> request) {
			try {
				String capturedPaymentId = objectMapper.writeValueAsString(request.get("capturedPaymentId"));
				Refund response = refundCapturedPayments(capturedPaymentId);
				return new ResponseEntity<>(response, HttpStatus.OK);
			} catch (Exception e) {
				e.printStackTrace();
				return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
			}
		}

		private Refund refundCapturedPayments(String capturedPaymentId) throws IOException, ApiException {
			PaymentsController paymentsController = client.getPaymentsController();
			CapturesRefundInput capturesRefundInput = new CapturesRefundInput.Builder(
					capturedPaymentId,
					null).build();
			ApiResponse<Refund> refundApiResponse = paymentsController.capturesRefund(capturesRefundInput);
			return refundApiResponse.getResult();
		}

		@PostMapping("/api/orders/{authorizationId}/captureAuthorize")
		public ResponseEntity<CapturedPayment> captureAuthorizeOrder(@PathVariable String authorizationId) {
			try {
				CapturedPayment response = captureAuthorizeOrders(authorizationId);
				return new ResponseEntity<>(response, HttpStatus.OK);
			} catch (Exception e) {
				e.printStackTrace();
				return new ResponseEntity<>(HttpStatus.INTERNAL_SERVER_ERROR);
			}
		}

		private CapturedPayment captureAuthorizeOrders(String authorizationId) throws IOException, ApiException {
			PaymentsController paymentsController = client.getPaymentsController();
			AuthorizationsCaptureInput authorizationsCaptureInput = new AuthorizationsCaptureInput.Builder(
					authorizationId,
					null).build();
			ApiResponse<CapturedPayment> authorizationsCapture = paymentsController
					.authorizationsCapture(authorizationsCaptureInput);
			return authorizationsCapture.getResult();
		}
	}
}