using UnityEngine;
using UnityEngine.UI;

public class TweenColor : TweenManager.Tween {

    private Color initialValues;
    private Image targetImage;
    private Color targetColor;

    public void Initialize(int id, Transform targetTransform, AnimationCurve animationCurve, Color targetColor, System.Action<TweenManager.Tween> managerCallBackFunction, System.Action callBackFunction) {
        if (animationCurve.length >= 2) {
            this.id = id;
            this.targetTransform = targetTransform;
            this.targetImage = targetTransform.GetComponent<Image>();
            this.initialValues = targetImage.color;
            this.animationCurve = animationCurve;
            this.targetColor = targetColor;
            this.beginTime = Time.time;
            this.endTime = Time.time + animationCurve.keys[animationCurve.length - 1].time;
            this.managerCallBackFunction = managerCallBackFunction;
            this.callBackFunction = callBackFunction;
        } else {
            Debug.LogWarning("The tween " + id + " does not have enough KeyFrames (" + animationCurve.length + ").");
            this.targetTransform = targetTransform;
            this.targetImage = targetTransform.GetComponent<Image>();
            this.initialValues = targetImage.color;
            this.endTime = 0f;
        }
    }

    void Update() {
        if (Time.time <= endTime) {
            float evaluation = Mathf.Clamp01(animationCurve.Evaluate((Time.time - beginTime) / (endTime - beginTime)));
            targetImage.color = (initialValues * (1 - evaluation)) + (targetColor * evaluation);
        } else {
            Stop();
        }
    }

    //Stops the tweening and returns to initialValues immediately
    public override void Stop() {
        targetImage.color = initialValues;
        managerCallBackFunction(this);
        if (callBackFunction != null) {
            callBackFunction();
        }
        Destroy(this);
    }
}
