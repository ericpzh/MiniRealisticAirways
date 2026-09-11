namespace MiniRealisticAirways;

public sealed class TutorialPage
{
	public string HeadingKey_ { get; }

	public string DescriptionKey_ { get; }

	public TutorialPage(string headingKey, string descriptionKey)
	{
		HeadingKey_ = headingKey;
		DescriptionKey_ = descriptionKey;
	}
}
