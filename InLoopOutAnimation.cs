using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace FRFuel
{
    public enum State
    {
        Starting,
        Looping,
        Ended
    }

    public struct Animation
    {
        public string dict;
        public string name;

        public Animation(string animationDictionary, string animationName)
        {
            dict = animationDictionary;
            name = animationName;
        }
    }

    public class InLoopOutAnimation
    {
        protected Animation start;
        protected Animation loop;
        protected Animation end;

        protected State state;

        public InLoopOutAnimation(Animation start, Animation loop, Animation end)
        {
            this.start = start;
            this.loop = loop;
            this.end = end;

            state = State.Ended;
        }

        public void Magick(Ped ped)
        {
            if (state == State.Ended)
            {
                PlayStart(ped);
                return;
            }

            if (state == State.Starting)
            {
                if (!IsAnimationPlaying(ped, start))
                {
                    state = State.Looping;
                    PlayLoop(ped);
                }
            }
        }

        protected void PlayStart(Ped ped)
        {
            ped.Task.PlayAnimation(
              start.dict,
              start.name,
              8f,
              -1,
              AnimationFlags.None
            );
            state = State.Starting;
        }

        protected void PlayLoop(Ped ped)
        {
            ped.Task.PlayAnimation(
              loop.dict,
              loop.name,
              50f,
              -1,
              AnimationFlags.Loop
            );
            state = State.Looping;
        }

        protected void PlayEnd(Ped ped)
        {
            ped.Task.PlayAnimation(
              end.dict,
              end.name,
              8f,
              -1,
              AnimationFlags.CancelableWithMovement
            );
            state = State.Ended;
        }

        public void RewindAndStop(Ped ped)
        {
            Function.Call(Hash.STOP_ENTITY_ANIM, ped.NativeValue, loop.name, loop.dict, true);

            PlayEnd(ped);
        }

        protected bool IsAnimationPlaying(Ped ped, Animation anim)
        {
            return Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, ped.NativeValue, anim.dict, anim.name, 3);
        }
    }
}
