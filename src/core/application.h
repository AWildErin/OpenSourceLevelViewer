#pragma once

#include <GLFW/glfw3.h>
#include "manager.h"

#include <vector>

// Main class for OSLV
class Application
{
public:
	Application();
	~Application();

	// Initialises and shutdowns OpenGL.
	// Takes the main brunt of the scaffolding
	void Init();
	void Shutdown(int exitType);

	// Returns bool for those managers that we have to check
	// 100% got added to our application
	bool AddManager(Manager* pManager);
	bool RemoveManager(Manager* pManager);

private:
	//void PreRender();
	void Render();
	//void PostRender();

	// Callbacks
	static void Callback_FrameBufferSize(GLFWwindow* window, int width, int height);

	std::vector<Manager*> pManagers;
	GLFWwindow* pWindow = nullptr;
};