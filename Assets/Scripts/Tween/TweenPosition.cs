using UnityEngine;

public class TweenPosition : TweenManager.Tween {

    private Vector3 initialValues;
    private AnimationCurve horizontalAnimationCurve;
    private AnimationCurve verticalAnimationCurve;

    public void Initialize(int id, Transform targetTransform, AnimationCurve horizontalAnimationCurve, AnimationCurve verticalAnimationCurve, System.Action<TweenManager.Tween> managerCallBackFunction, System.Action callBackFunction) {
        if (horizontalAnimationCurve.length >= 2 && verticalAnimationCurve.length >= 2) {
            this.id = id;
            this.targetTransform = targetTransform;
            this.initialValues = targetTransform.position;
            this.horizontalAnimationCurve = horizontalAnimationCurve;
            this.verticalAnimationCurve = verticalAnimationCurve;
            this.beginTime = Time.time;
            this.endTime = Time.time + Mathf.Min(horizontalAnimationCurve.keys[horizontalAnimationCurve.length - 1].time, verticalAnimationCurve.keys[verticalAnimationCurve.length - 1].time);
            this.managerCallBackFunction = managerCallBackFunction;
            this.callBackFunction = callBackFunction;
        } else {
            Debug.LogWarning("The tween " + id + " does not have enough KeyFrames.");
            this.targetTransform = targetTransform;
            this.initialValues = targetTransform.position;
            this.endTime = 0f;
        }
    }

    void Update() {
        if (Time.time <= endTime) {
            float x = initialValues.x * (horizontalAnimationCurve.Evaluate((Time.time - beginTime) / (endTime - beginTime)));
            float y = initialValues.y * (verticalAnimationCurve.Evaluate((Time.time - beginTime) / (endTime - beginTime)));
            targetTransform.position = new Vector3(x, y, 0f);
        } else {
            Stop();
        }
    }

    //Stops the tweening and returns to initialValues immediately
    public override void Stop() {
        targetTransform.position = initialValues;
        managerCallBackFunction(this);
        if (callBackFunction != null) {
            callBackFunction();
        }
        Destroy(this);
    }
}
