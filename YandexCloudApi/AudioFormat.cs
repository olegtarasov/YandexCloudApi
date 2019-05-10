namespace YandexCloudApi
{
    /// <summary>
    /// Audio format.
    /// </summary>
    public enum AudioFormat
    {
        /// <summary>
        /// Raw WAV data without header.
        /// </summary>
        PCM,

        /// <summary>
        /// OPUS-encoded sound wrapped into OGG container.
        /// </summary>
        OggOpus
    }
}