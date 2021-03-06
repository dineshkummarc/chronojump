dnl Warning: This is an automatically generated file, do not edit!
dnl Process this file with autoconf to produce a configure script.
AC_PREREQ([2.54])
AC_INIT([chronojump], [0.9.4])
AM_INIT_AUTOMAKE([foreign])

AC_CONFIG_MACRO_DIR([m4])

AM_PROG_LIBTOOL

AM_PATH_PYTHON

dnl Check for pkg-config
AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
        AC_MSG_ERROR([You need to install pkg-config])
fi

SHAMROCK_EXPAND_LIBDIR
SHAMROCK_EXPAND_BINDIR
SHAMROCK_EXPAND_DATADIR


AC_PROG_INSTALL

dnl Check for C# compiler
AC_PATH_PROG(CSC, csc, no)
if test "x$CSC" = "xno"; then
	AC_PATH_PROG(MCS, gmcs, no)
	if test "x$MCS" = "xno"; then
       		AC_PATH_PROG(MCS2, gmcs2,no)
		if test "x$MCS2" = "xno"; then
			AC_MSG_ERROR([You need to install a C-sharp compiler])
		else 
			AC_SUBST(GMCS,[gmcs2])	
		fi
	
	else 
		AC_SUBST(GMCS,[gmcs])
	fi
else 
	AC_SUBST(GMCS,[csc])
fi

dnl Check for gettext utils
AC_PATH_PROG(MSGFMT, msgfmt, no)
if test "x$MSGFMT" = "xno"; then
	AC_MSG_ERROR([gettext not found])
else 
	AC_SUBST(MSGFMT,[msgfmt])
fi


AC_ARG_ENABLE(debug,
	AC_HELP_STRING([--enable-debug],
		[Use 'DEBUG' Configuration [default=NO]]),
		enable_debug=yes, enable_debug=no)
AM_CONDITIONAL(ENABLE_DEBUG, test x$enable_debug = xyes)
if test "x$enable_debug" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
AC_ARG_ENABLE(release,
	AC_HELP_STRING([--enable-release],
		[Use 'RELEASE' Configuration [default=YES]]),
		enable_release=yes, enable_release=no)
AM_CONDITIONAL(ENABLE_RELEASE, test x$enable_release = xyes)
if test "x$enable_release" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
if test -z "$CONFIG_REQUESTED" ; then
	AM_CONDITIONAL(ENABLE_RELEASE, true)
	enable_release=yes
fi


dnl package checks, common for all configs
PKG_CHECK_MODULES([GLIB_SHARP_20], [glib-sharp-2.0])
PKG_CHECK_MODULES([GTK_SHARP_20], [gtk-sharp-2.0])
PKG_CHECK_MODULES([GLADE_SHARP_20], [glade-sharp-2.0])
PKG_CHECK_MODULES([WEBKIT_SHARP_10], [webkit-sharp-1.0])

PKG_CHECK_MODULES(CESARPLAYER, [gtk+-2.0 >= 2.8 gdk-2.0 gio-2.0 glib-2.0 gstreamer-0.10 gstreamer-audio-0.10 gstreamer-video-0.10 gstreamer-pbutils-0.10 gobject-2.0 gstreamer-interfaces-0.10])
AC_SUBST(CESARPLAYER_CFLAGS)
AC_SUBST(CESARPLAYER_LIBS)

dnl Check for Mono assemblies that we need
needed_dlls="Mono.Data.Sqlite Mono.Posix"
for i in $needed_dlls; do
  AC_MSG_CHECKING([for $i.dll])
  if test ! \( -e `$PKG_CONFIG --variable=prefix mono`/lib/mono/2.0/$i.dll -o -e `$PKG_CONFIG --variable=prefix mono`/lib64/mono/2.0//$i.dll \); then
    AC_MSG_ERROR([missing required mono 2.0 DLL: $i.dll])
  else
    AC_MSG_RESULT([found])
  fi
done


AC_CONFIG_FILES([
Makefile
chronopic-firmware/Makefile
chronopic-firmware/chronopic-firmwarecord/Makefile
chronopic-firmware/chronopic-firmwarecord/libIris/Makefile
chronopic-firmware/chronopic-firmwarecord/chronopic-firmwarecord
chronopic-tests/Makefile
src/Makefile
po/Makefile
chronojump_server/Makefile
manual/Makefile
src/AssemblyInfo.cs
src/chronojump
src/chronojump_mini
src/chronojump.desktop
libcesarplayer/Makefile
libcesarplayer/src/Makefile
CesarPlayer/Makefile
CesarPlayer/AssemblyInfo.cs
CesarPlayer/CesarPlayer.dll.config
])

AC_OUTPUT
