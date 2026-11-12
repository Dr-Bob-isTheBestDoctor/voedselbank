using Microsoft.AspNetCore.Components.Rendering;
public class AfbeeldingOproep()
{
    
}
public class TextOproep()
{
    public static void Oproep(RenderTreeBuilder builder,
        string name,
        ref int seq)
    {
        //base div
        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "TextOproep");
        //oproepbanner div
        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "OproepBanner");
        //oproepbanner div name
        builder.OpenElement(seq++, "h1");
        builder.AddContent(seq++, name);
        //close oproepbanner div name
        builder.CloseElement();
        //close oproepbanner div
        builder.CloseElement();
        //Oproep info div
        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "TxtOproepInfo");
        //Date info
        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "vline");
        //Date Text
        builder.OpenElement(seq++, "h1");
        builder.AddAttribute(seq++, "class", "datumtext");
        builder.AddContent(seq++, "Donderdag 14:00-16:00");
        //close date text
        builder.CloseElement();
        ///close date info div
        builder.CloseElement();
        //open position text
        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "positionsavailable");
        builder.OpenElement(seq++, "h1");
        builder.AddAttribute(seq++, "class", "positionTxt");
        builder.AddContent(seq++, "1 van 4 plekken bezet");
        //exit position txt
        builder.CloseElement();
        //register button
        builder.OpenElement(seq++, "button");
        builder.AddAttribute(seq++, "class", "Registerbtn");
        builder.AddContent(seq++, "Aanmelden");
        //close button div
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
        //close base div
        builder.CloseElement();
    }
}