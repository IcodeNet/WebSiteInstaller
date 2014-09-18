If you get into trouble with your installer, it may help to run it with logging turned on.

To do so, install your package from a command prompt using msiexec with the arguments /l*v,
and the name of a file to write the log to. For example, if you had an installer called:

myInstaller.msi, 
  
you could use this command to write a log during the installation to a file called myLog.txt:

**********************************************
msiexec /i myInstaller.msi /l*v myLog.txt
**********************************************

It works for uninstalls too. Simply use the /x argument instead of /i. 
The log can be pretty helpful, but also very verbose. 
If your installer fails midway through, you might try searching the log for the text return value 3. 
This indicates that an action returned a status of failure. 

Often, you'll also see a specific MSI error code. 
You can find its meaning by searching for that number in the MSI SDK Documentation help file that comes with WiX.

Note
You can also turn on logging for all MSI packages by editing the 
HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows\Installer key in the Windows Registry. 

This should be used with care though so as not to use too much disk space. 
See http://support.microsoft.com/kb/223300 for more information.