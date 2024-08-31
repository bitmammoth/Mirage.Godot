using System.Text;
using Godot;
using Nethereum.Signer;
using Nethereum.Util;
using Newtonsoft.Json;
public class Web3Component()
{
    public string CryptoAddress { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    private readonly string _url = "http://localhost:5284/?message=";
    public static int GetUnixTimestampNow() => (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
    public static int GetExpirationTime() => GetUnixTimestampNow() + 60;
    public int OriginalMessage { get; set; }
    public async Task<CryptoLoginMessage> GetLoginMessage()
    {
        OriginalMessage = GetExpirationTime();
        GD.Print($"Set OriginalMessage: {OriginalMessage}");
        var cryptoLoginMessage = await Sign(OriginalMessage);
        //GD.Print($"CryptoLoginMessage: {cryptoLoginMessage.CryptoAddress} {cryptoLoginMessage.Hash} {cryptoLoginMessage.Signature} {cryptoLoginMessage.Timestamp} {cryptoLoginMessage.TokenString} {cryptoLoginMessage.TokenString.Length} {cryptoLoginMessage.Hash} {cryptoLoginMessage.Hash.Length}");
        if (cryptoLoginMessage.CryptoAddress.Length != 42)
        {
            GD.PrintErr("Web 3 CRYPTO Address is not valid!");
            return default;
        }
        if (cryptoLoginMessage.Signature.Length != 132)
        {
            GD.PrintErr("Web 3 CRYPTO Signature is not valid!");
            return default;
        }
        Signature = cryptoLoginMessage.Signature;
        Token = cryptoLoginMessage.TokenString;
        Hash = cryptoLoginMessage.Hash;
        //GD.Print($"JSON: {cryptoLoginMessage.CryptoAddress} {cryptoLoginMessage.Hash} {cryptoLoginMessage.Signature} {cryptoLoginMessage.Timestamp} {cryptoLoginMessage.TokenString} {cryptoLoginMessage.TokenString.Length} {cryptoLoginMessage.Hash} {cryptoLoginMessage.Hash.Length}");
        DisplayServer.ClipboardSet("");
        return cryptoLoginMessage;
    }
    public bool IsLoginMessageValid(CryptoLoginMessage message, int currentTime)
    {
        if (message.CryptoAddress.Length != 42)
        {
            GD.PrintErr("Web 3 CRYPTO Address is not valid!");
            return false;
        }
        CryptoAddress = VerifySignature(message.Signature, message.Timestamp);
        GD.Print($"Account: {CryptoAddress}");
        return CryptoAddress.Length == 42 && int.Parse(message.Timestamp) >= currentTime;
    }
    public async Task<CryptoLoginMessage> CryptoLogin()
    {
        var loginMessage = await GetLoginMessage();

        if (!IsLoginMessageValid(loginMessage, OriginalMessage))
        {
            GD.PrintErr("Web 3 CRYPTO Message is not valid!");
            return default;
        }
        return loginMessage;
    }
    public async Task<CryptoLoginMessage> Sign(int currentTime)
    {
        try
        {
            var message = Uri.EscapeDataString(currentTime.ToString());
            var signerEndpoint = _url + message;
            DisplayServer.ClipboardSet("");
            OS.ShellOpen(signerEndpoint);
            return await GetClipboardContentWithRetry();
        }
        catch (Exception ex)
        {
            GD.PrintErr(ex.Message);
            throw;
        }
    }
    private async Task<CryptoLoginMessage> GetClipboardContentWithRetry()
    {
        var timeoutInSeconds = 30;
        var elapsedSeconds = 0;
        var clipboardContent = string.Empty;
        var loginMessage = new CryptoLoginMessage();
        var isValid = false;

        while (elapsedSeconds < timeoutInSeconds)
        {
            try
            {
                clipboardContent = DisplayServer.ClipboardGet();
                if (!string.IsNullOrEmpty(clipboardContent))
                {
                    // Attempt to deserialize the clipboard content into a CryptoLoginMessage object
                    loginMessage = JsonConvert.DeserializeObject<CryptoLoginMessage>(clipboardContent);

                    // Validate the deserialized message
                    if (ValidateCryptoLoginMessage(loginMessage))
                    {
                        isValid = true;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr("Clipboard error:", ex.Message);
            }

            // Wait for 1 second before retrying
            await Task.Delay(1000);
            elapsedSeconds++;
        }

        // If no valid message was retrieved within the timeout period, throw an exception or handle it as needed
        if (!isValid)
        {
            throw new Exception("Failed to retrieve a valid CryptoLoginMessage from the clipboard within the timeout period.");
        }

        return loginMessage;
    }

    private static bool ValidateCryptoLoginMessage(CryptoLoginMessage message)
    {
        // Perform basic validation of the CryptoLoginMessage content
        return !string.IsNullOrEmpty(message.Signature) &&
               !string.IsNullOrEmpty(message.CryptoAddress) &&
               !string.IsNullOrEmpty(message.Hash) &&
               !string.IsNullOrEmpty(message.Timestamp) &&
               !string.IsNullOrEmpty(message.TokenString);
    }

    public static DateTime HexToDateTime(string hexValue)
    {
        var unixTimestamp = Convert.ToInt64(hexValue, 16);
        DateTime origin = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        return origin.AddSeconds(unixTimestamp);
    }
    public static string VerifySignature(string signatureString, string originalMessage)
    {
        // Convert the original message (timestamp) to a string.
        var message = originalMessage.ToString();

        // Properly format the message with the Ethereum signed message prefix and the length of the original message.
        var prefix = "\x19" + "Ethereum Signed Message:\n" + message.Length;
        var prefixedMessage = prefix + message;

        //GD.Print($"Prefixed Message: {prefixedMessage}");
        EthECKey key;
        EthECDSASignature signature;
        byte[] bytes;

        try
        {
            // Hash the properly formatted message.
            bytes = new Sha3Keccack().CalculateHash(Encoding.UTF8.GetBytes(prefixedMessage));
            signature = MessageSigner.ExtractEcdsaSignature(signatureString);
            key = EthECKey.RecoverFromSignature(signature, bytes);
        }
        catch (Exception ex)
        {
            GD.PrintErr("Error verifying message (EthereumSignatureVerifier): ", ex.Message);
            throw;
        }

        return key.GetPublicAddress();
    }

}