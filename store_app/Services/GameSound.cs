namespace store_app.Services;

public static class GameSound
{
#if IOS || MACCATALYST
    static AVFoundation.AVAudioPlayer? pointPlayer;
#endif

    public static void PlayPoint()
    {
        try
        {
#if ANDROID
            Android.Media.MediaPlayer? player = null;

            try
            {
                using var asset = Android.App.Application.Context.Assets?.OpenFd("sfx_point.wav");
                if (asset is null)
                {
                    return;
                }

                player = new Android.Media.MediaPlayer();
                player.SetDataSource(asset.FileDescriptor, asset.StartOffset, asset.Length);
                player.Prepare();
                player.Completion += (_, _) =>
                {
                    player.Release();
                    player.Dispose();
                };
                player.Start();
            }
            catch
            {
                player?.Release();
                player?.Dispose();
            }
#elif IOS || MACCATALYST
            var url = Foundation.NSBundle.MainBundle.GetUrlForResource("sfx_point", "wav");
            if (url is null)
            {
                return;
            }

            pointPlayer?.Stop();
            var player = AVFoundation.AVAudioPlayer.FromUrl(url);
            if (player is null)
            {
                return;
            }

            pointPlayer = player;
            player.Volume = 0.7f;
            player.PrepareToPlay();
            player.Play();
#endif
        }
        catch
        {
            // Sound is optional; gameplay should never fail because audio is unavailable.
        }
    }
}
