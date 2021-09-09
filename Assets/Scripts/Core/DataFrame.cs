using UnityEngine;


namespace StrokeMimicry
{
    public class DataFrame
    {
        public double Timestamp = 0.0;
        public Vector3 HeadPosition = Vector3.zero;
        public Vector3 PenPosition = Vector3.zero;
        public Vector3 HeadUp = Vector3.up;
        public Vector3 HeadForward = Vector3.forward;
        public Vector3 ControllerUp = Vector3.up;
        public Vector3 ControllerForward = Vector3.forward;
        
        //public DataFrame()
        //{

        //}

        public DataFrame(double t, Vector3 hp, Vector3 pp, Vector3 hu, Vector3 hf, Vector3 cu, Vector3 cf)
        {
            Timestamp = t;
            HeadPosition = hp;
            PenPosition = pp;
            HeadUp = hu;
            HeadForward = hf;
            ControllerUp = cu;
            ControllerForward = cf;
        }

        public DataFrame(DataFrame orig)
        {
            Timestamp = orig.Timestamp;
            HeadPosition = orig.HeadPosition;
            PenPosition = orig.PenPosition;
            HeadUp = orig.HeadUp;
            HeadForward = orig.HeadForward;
            ControllerUp = orig.ControllerUp;
            ControllerForward = orig.ControllerForward;
        }

        //public void SetAll(double t, Vector3 hp, Vector3 pp, Vector3 hu, Vector3 hf, Vector3 cu, Vector3 cf, Vector3 sd)
        //{
        //    Timestamp = t;
        //    HeadPosition = hp;
        //    PenPosition = pp;
        //    HeadUp = hu;
        //    HeadForward = hf;
        //    ControllerUp = cu;
        //    ControllerForward = cf;
        //    SprayDirection = sd;
        //}

        public static DataFrame WorldToLocal(DataFrame orig, Transform transform)
        {
            //var transform = ModelsController.CurrentModel.transform;

            return new DataFrame
            (
                orig.Timestamp,
                transform.InverseTransformPoint(orig.HeadPosition),
                transform.InverseTransformPoint(orig.PenPosition),
                transform.InverseTransformDirection(orig.HeadUp),
                transform.InverseTransformDirection(orig.HeadForward),
                transform.InverseTransformDirection(orig.ControllerUp),
                transform.InverseTransformDirection(orig.ControllerForward)
            );
        }

        public static DataFrame LocalToWorld(DataFrame orig, Transform transform)
        {
            //var transform = ModelsController.CurrentModel.transform;

            return new DataFrame
            (
                orig.Timestamp,
                transform.TransformPoint(orig.HeadPosition),
                transform.TransformPoint(orig.PenPosition),
                transform.TransformDirection(orig.HeadUp),
                transform.TransformDirection(orig.HeadForward),
                transform.TransformDirection(orig.ControllerUp),
                transform.TransformDirection(orig.ControllerForward)
            );
        }
    }

}
