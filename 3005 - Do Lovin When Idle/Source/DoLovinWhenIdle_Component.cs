using Verse;

namespace eqdseq
{
    public class DoLovinWhenIdle_Component : GameComponent
    {
        private Game unusedMyComponent;
        public DoLovinWhenIdle_Component(Game game)
        {
            unusedMyComponent = game;
            DLWI_DictionaryField_Manager.Reset();
        }
        public override void LoadedGame()
        {
            unusedMyComponent.components.Remove(this);
        }
        public override void ExposeData()
        {

        }
    }
}