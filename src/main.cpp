#include "core/application.h"

#include "demo/hellotriangle.h"

int main()
{
	// Create the application
	Application* app = new Application();

	app->AddManager(new HelloTriangle);

	// Init must be called last as this fires off the rest of
	// the GL stuff. Meaning our UI objects and the like must be added 
	app->Init();
}