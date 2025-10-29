namespace DuckovLuckyBox.Core
{
    interface IComponent
    {
        void Toggle();
        void Open();
        void Close();
        void Destroy();
    }
}