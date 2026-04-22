namespace SkiRental.App;

public partial class App : Application
{
    // Konstruktor przyjmuje już wstrzyknięty MainPage
    public App(MainPage mainPage)
    {
        InitializeComponent();

        // Ustawiamy przekazany obiekt, zamiast tworzyć go przez 'new'
        MainPage = mainPage;
    }
}