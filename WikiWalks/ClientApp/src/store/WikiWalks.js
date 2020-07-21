const initializeType = 'INITIALIZE';
const receivePagesType = 'RECEIVE_PAGES';
const initialState = { pages: {} };

export const actionCreators = {
    requestPagesForTheTitle: wordId => async (dispatch, getState) => {
        try {
            const url = `api/WikiWalks/getRelatedArticles?wordId=${wordId}`;
            const response = await fetch(url);
            const pages = await response.json();

            if (!pages || !pages.pages || pages.pages.length < 5) window.location.href = `/not-found?p=${window.location.pathname}`;

            dispatch({ type: receivePagesType, pages });
        } catch (e) {
            window.location.href = `/not-found?p=${window.location.pathname}`;
        }
    },
    initialize: () => (dispatch, getState) => {
        dispatch({ type: initializeType });
    }
};

export const reducer = (state, action) => {
    state = state || initialState;

    if (action.type === initializeType) {
        return initialState;
    }

    if (action.type === receivePagesType) {
        return {
            ...state,
            pages: action.pages,
        };
    }

    return state;
};
