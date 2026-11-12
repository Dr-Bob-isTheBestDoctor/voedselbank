using Microsoft.AspNetCore.Components.Rendering;

public class Website()
{
    public int OproepCount = 0;
    public static void MaakOproep(bool afbeelding, string name, string description, RenderTreeBuilder builder, ref int seq)
    {
        if(afbeelding)
        {
            new AfbeeldingOproep();
        }
        else
        {
            TextOproep.Oproep(builder, name, ref seq);
        }
    }
}

//oproep classes
//MaakOproep maakt de class aan
//class maakt de div's die nodig zijn met de parameters
