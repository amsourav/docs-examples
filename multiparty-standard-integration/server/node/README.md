# Multiparty Standard Integration Node.js Sample

PayPal Multiparty Standard Integration sample in Node.js

## Running the sample

1. Add your API credentials to the environment:

   - **Windows (powershell)**

     ```powershell
      $env:PAYPAL_CLIENT_ID = "<PAYPAL_CLIENT_ID>"
      $env:PAYPAL_CLIENT_SECRET = "<PAYPAL_CLIENT_SECRET>"
      $env:PAYPAL_SELLER_PAYER_ID = "<PAYPAL_SELLER_PAYER_ID>"
      $env:PAYPAL_BN_CODE = "<PAYPAL_BN_CODE>"
     ```

   - **Linux / MacOS**

     ```bash
      export PAYPAL_CLIENT_ID="<PAYPAL_CLIENT_ID>"
      export PAYPAL_CLIENT_SECRET="<PAYPAL_CLIENT_SECRET>"
      export PAYPAL_SELLER_PAYER_ID="<PAYPAL_SELLER_PAYER_ID>"
      export PAYPAL_BN_CODE="<PAYPAL_BN_CODE>"
     ```

2. Install the packages

   ```bash
   npm install
   ```

3. Run the server

   ```bash
   npm run start
   ```

4. Go to [http://localhost:8080/](http://localhost:8080/)