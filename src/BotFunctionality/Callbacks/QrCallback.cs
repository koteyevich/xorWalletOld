using QRCoder;
using Telegram.Bot;
using Telegram.Bot.Types;
using xorWallet.Interfaces;
using xorWallet.Utils;

namespace xorWallet.Callbacks
{
    public class QrCallback : ICallback
    {
        public string Name => "qr";

        public string[] Aliases => [];

        public async Task ExecuteAsync(CallbackQuery callbackQuery, TelegramBotClient bot, string data)
        {
            string[] parts = data.Split('_');
            string startData = string.Join("_", parts.Skip(1));

            PayloadGenerator.Url generator = new PayloadGenerator.Url(StartUrlGenerator.GenerateStartUrl(startData));
            string payload = generator.ToString();

            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Default);
            BitmapByteQRCode qrCode = new BitmapByteQRCode(qrCodeData);
            byte[] qrCodeString = qrCode.GetGraphic(5);

            //! memory stream is not the best way of doing this... but i think i can get away with it because the image is like ~350x350x
            using var ms = new MemoryStream(qrCodeString);
            await bot.SendPhoto(callbackQuery.Message!.Chat.Id, InputFile.FromStream(ms));
        }
    }
}
