window.getUserLang = () => {
    const l = navigator.language || navigator.userLanguage;
    document.documentElement.lang = l;
    return l;
};