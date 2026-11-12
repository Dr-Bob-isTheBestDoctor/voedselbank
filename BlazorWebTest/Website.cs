

public class Website()
{
    public int OproepCount = 0;
    void MaakOproep(bool afbeelding, string name, string description)
    {
        if(afbeelding)
        {
            new AfbeeldingOproep();
        }
        else
        {
            new TextOproep(name, description);
        }
    }
}