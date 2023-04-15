namespace Tarik.UnitTests;

public static class ApprovedPlans
{
    public const string CalculatorPlan = """
    ## Step-by-step discussion

    To implement a basic calculator app, we need to create the necessary files for the app structure, design the user interface, and implement the functionality for basic arithmetic operations and on-screen numeric keypad input.

    ## Plan

    1. Create and populate the file /index.html | To create the main HTML file for the calculator app | []
    2. Create and populate the file /styles.css | To create the CSS file for styling the calculator app | ["/index.html"]
    3. Create and populate the file /scripts.js | To create the JavaScript file for implementing the calculator app functionality | ["/index.html"]
    4. Edit the file /index.html | To design the user interface for the calculator app, including the display and on-screen numeric keypad | ["/styles.css", "/scripts.js"]
    5. Edit the file /styles.css | To style the user interface elements of the calculator app, such as the display, buttons, and layout | ["/index.html"]
    6. Edit the file /scripts.js | To implement the functionality for basic arithmetic operations (add, subtract, multiply, divide) and on-screen numeric keypad input | ["/index.html"]
    7. Edit the file /README.md | To update the README file with information about the implemented calculator app, its features, and usage instructions | ["/index.html", "/styles.css", "/scripts.js"]
    """;
}
