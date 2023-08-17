using System.Threading.Tasks;

namespace EhViewer
{
    public class Limit
    {
        public Limit(int Max)
        {
            this.Max = Max;
        }
        public int Max = 0;
        public volatile int Value = 0;
        public async Task WaitForAvaliable()
        {
            while (Value >= Max)
            {
                await Task.Delay(20);
            }
        }
        public async Task Enter()
        {
            await WaitForAvaliable();
            Value++;
        }
        public void Exit()
        {
            Value--;
        }
    }
}
