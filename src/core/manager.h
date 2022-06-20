#pragma once

/// <summary>
/// Defines the base class for any "manager" object.
/// e.g. WorldManager, ImGuiManager etc.
/// </summary>
class Manager
{
public:
	virtual void PreInitialise() { };
	virtual void Initialise() { };
	
	virtual void PreRender() { };
	virtual void Render() { };
	virtual void PostRender() { };

	virtual void Shutdown() { };
};