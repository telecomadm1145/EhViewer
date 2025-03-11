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
                await Task.Delay(1);
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
        public static Limit g_limit = new(5);
    }
}
