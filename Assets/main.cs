using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// the script is attached to the main camera
public class Main : MonoBehaviour {
	// character controls
    CharacterController playerController;
	bool controlling = false;
	float sensitivity = 10;
	float rotX = 0;
	float rotY = 0;
	float v = 0.1F;
	float vy = 0;
	float g = 9.81F;
	float jump = 5;

	// drone
	GameObject drone;
	Rigidbody droneRb;
	GameObject dronePx;
	GameObject dronePz;
	bool activate = false;
	// z
	// |
	// f0, f1
	// f2, f3 __ x
	Pid pidY;
	Pid pidRx;
	Pid pidRz;
	
	//ui
	UIDocument ui;
	TextElement activate_label;
	TextElement debug_label;

	void Start() {
		transform.position = new Vector3(1, 1, -7);
        playerController = GetComponent<CharacterController>();

		drone = GameObject.Find("Drone1");
		drone.transform.Rotate(new Vector3(14, 14, 14));
		droneRb = drone.GetComponent<Rigidbody>();
		dronePx = new GameObject("px");
		dronePx.transform.SetParent(drone.transform);
		dronePx.transform.localPosition = new Vector3(0, 0, 1);
		dronePz = new GameObject("pz");
		dronePz.transform.SetParent(drone.transform);
		dronePz.transform.localPosition = new Vector3(1, 0, 0);

		ui = FindObjectOfType<UIDocument>();
		activate_label = ui.rootVisualElement.Q<TextElement>("activate");
		activate_label.text = string.Format("activated: {0}", activate);
		debug_label = ui.rootVisualElement.Q<TextElement>("debug");
	}


	void Update() {
		if(controlling) {
			// clear debug text
			debug_label.text = "debug:\n";

			// character view control
			// rotate
			rotY += Input.GetAxis("Mouse X") * sensitivity;
			rotX -= Input.GetAxis("Mouse Y") * sensitivity;
			rotX = Mathf.Clamp(rotX, -80, 80);
			transform.eulerAngles = new Vector3(rotX, rotY, 0);
			// translate
			if(playerController.isGrounded) {
				if(Input.GetKeyDown(KeyCode.Space)) {
					vy = jump;
				} else {
					vy = 0;
				}
			} else {
				vy -= Time.deltaTime * g;
			}
			var vy3 = new Vector3(0, vy * Time.deltaTime, 0);
			float keyX = 0;
			if(Input.GetKey(KeyCode.D)) {
				keyX += v;
			}
			if(Input.GetKey(KeyCode.A)) {
				keyX -= v;
			}
			float keyZ = 0;
			if(Input.GetKey(KeyCode.W)) {
				keyZ += v;
			}
			if(Input.GetKey(KeyCode.S)) {
				keyZ -= v;
			}
			float rotYRad = Angle2Rad(rotY);
			var verV = new Vector3(keyZ * Mathf.Sin(rotYRad), 0, keyZ * Mathf.Cos(rotYRad));
			var horV = new Vector3(keyX * Mathf.Cos(rotYRad), 0, -keyX * Mathf.Sin(rotYRad));
			playerController.Move(horV + verV + vy3);

			// drone control
			// direct control
			// if(Input.GetKeyDown(KeyCode.F)) {
			// 	activate = !activate;
			// 	activate_label.text = string.Format("activated: {0}", activate);
			// }
			// if(activate) {
			// 	f0 = f1 = f2 = f3 = 0.20F * g;
			// } else {
			// 	f0 = f1 = f2 = f3 = 0;
			// }
			// float forward = 0;
			// if(Input.GetKey(KeyCode.UpArrow)) {
			// 	forward += 1;
			// }
			// if(Input.GetKey(KeyCode.DownArrow)) {
			// 	forward -= 1;
			// }
			// float right = 0;
			// if(Input.GetKey(KeyCode.RightArrow)) {
			// 	right += 1;
			// }
			// if(Input.GetKey(KeyCode.LeftArrow)) {
			// 	right -= 1;
			// }
			// float up = 0;
			// if(Input.GetKey(KeyCode.Return)) {
			// 	up += 2;
			// }
			// if(Input.GetKey(KeyCode.LeftShift)) {
			// 	up -= 2;
			// }
			// f0 += (forward - right + up) * df;
			// f1 += (forward + right + up) * df;
			// f2 += (-forward - right + up) * df;
			// f3 += (-forward + right + up) * df;
			
			// droneRb.AddRelativeForce(new Vector3(0, f0 + f1 + f2 + f3, 0));
			// droneRb.AddRelativeTorque(new Vector3((f0 + f1 - f2 - f3) * arm, 0, 0));
			// droneRb.AddRelativeTorque(new Vector3(0, 0, -(f1 + f3 - f0 - f2) * arm));
			
			// pid control
			if(Input.GetKeyDown(KeyCode.F)) {
				activate = !activate;
				activate_label.text = string.Format("activated: {0}", activate);
				if(activate) {
					droneRb.AddRelativeForce(new Vector3(0, 5, 0), ForceMode.Impulse);
					pidY = new(droneRb.velocity.y, 5.0F, 3.0F, 4.0F);
					float dxy = drone.transform.position.y - dronePx.transform.position.y;
					pidRx = new(dxy, 3.0F, 0.03F, 10.0F);
					float dzy = dronePz.transform.position.y - drone.transform.position.y;
					pidRz = new(dzy, 3.0F, 0.03F, 10.0F);
				}
			}
			if(activate) {
				float fUp = 0;
				if(Input.GetKey(KeyCode.Return)) {
					fUp = MathF.Max(pidY.Step(1.0F, droneRb.velocity.y), 0);
				} else if(Input.GetKey(KeyCode.RightShift)) {
					fUp = MathF.Max(pidY.Step(-1.0F, droneRb.velocity.y), 0);
				} else {
					if(droneRb.velocity.y < 0.2) {
						fUp = MathF.Max(pidY.Step(0, droneRb.velocity.y), 0);
					};
				}
				droneRb.AddRelativeForce(new Vector3(0, fUp, 0));
				float dxy = drone.transform.position.y - dronePx.transform.position.y;
				float mX;
				if(Input.GetKey(KeyCode.UpArrow)) {
					mX = pidRx.Step(0.4F, dxy);
				} else if(Input.GetKey(KeyCode.DownArrow)) {
					mX = pidRx.Step(-0.4F, dxy);
				} else {
					mX = pidRx.Step(0, dxy);
				}
				float dzy = dronePz.transform.position.y - drone.transform.position.y;
				float mZ;
				if(Input.GetKey(KeyCode.RightArrow)) {
					mZ = pidRz.Step(-0.4F, dzy);
				} else if(Input.GetKey(KeyCode.LeftArrow)) {
					mZ = pidRz.Step(0.4F, dzy);
				} else {
					mZ = pidRz.Step(0, dzy);
				}
				droneRb.AddRelativeTorque(new Vector3(mX, 0, mZ));
			}


			// exit controlling
			if(Input.GetKeyDown(KeyCode.Escape)) {
				UnityEngine.Cursor.visible = true;
				UnityEngine.Cursor.lockState = CursorLockMode.None;
				controlling = false;
			}
		} else {
			// entering controlling
			if(Input.GetButtonDown("Fire1")) {
				UnityEngine.Cursor.visible = false;
				UnityEngine.Cursor.lockState = CursorLockMode.Locked;
				controlling = true;
			}
		}
	}

	float Angle2Rad(float angle) {
		return angle / 180 * Mathf.PI;
	}
}


class Pid {
	float kp;
	float ki;
	float kd;
	float acc = 0;
	float last;
	public Pid(float start, float p, float i, float d) {
		kp = p;
		ki = i;
		kd = d;
		last = start;
	}
	public float Step(float target, float current) {
		float e = target - current;
		acc += e;
		float u = kp * e + ki * acc + kd * (e - last);
		last = e;
		return u;
	}
	public void ClearAcc() {
		acc = 0;
	}
}